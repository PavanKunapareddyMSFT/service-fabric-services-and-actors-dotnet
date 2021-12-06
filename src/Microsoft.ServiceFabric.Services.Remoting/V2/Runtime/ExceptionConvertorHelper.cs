// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V2.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;
    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;

    internal class ExceptionConvertorHelper
    {
        private IEnumerable<IExceptionConvertor> convertors;
        private FabricTransportRemotingListenerSettings listenerSettings;

        public ExceptionConvertorHelper(IEnumerable<IExceptionConvertor> convertors, FabricTransportRemotingListenerSettings listenerSettings)
        {
            this.convertors = convertors;
            this.listenerSettings = listenerSettings;
        }

        public ServiceException ToServiceException(Exception originalException, int currentDepth)
        {
            ServiceException serviceException = null;
            foreach (var convertor in this.convertors)
            {
                try
                {
                    if (convertor.TryConvertToServiceException(originalException, out serviceException))
                    {
                        if (++currentDepth > this.listenerSettings.RemotingExceptionDepth)
                        {
                            break;
                        }

                        var innerEx = convertor.GetInnerExceptions(originalException);
                        if (innerEx != null && innerEx.Length > 0)
                        {
                            serviceException.ActualInnerExceptions = new List<ServiceException>();
                            int currentBreadth = 0;
                            foreach (var inner in innerEx)
                            {
                                if (++currentBreadth > this.listenerSettings.RemotingExceptionDepth)
                                {
                                    break;
                                }

                                serviceException.ActualInnerExceptions.Add(this.ToServiceException(inner, currentDepth));
                            }
                        }

                        break;
                    }
                }
                catch (Exception)
                {
                    // Throw
                }
            }

            return serviceException;
        }

        public ServiceException ToServiceException(Exception originalException)
        {
            return this.ToServiceException(originalException, 1);
        }

        public RemoteException2 ToRemoteException(ServiceException serviceException)
        {
            var remoteException = new RemoteException2()
            {
                Message = serviceException.Message,
                Type = serviceException.ActualExceptionType,
                StackTrace = serviceException.ActualExceptionStackTrace,
                Data = serviceException.ActualExceptionData,
            };

            if (serviceException.ActualInnerExceptions != null && serviceException.ActualInnerExceptions.Count > 0)
            {
                remoteException.InnerExceptions = new List<RemoteException2>();
                foreach (var inner in serviceException.ActualInnerExceptions)
                {
                    remoteException.InnerExceptions.Add(this.ToRemoteException(inner));
                }
            }

            return remoteException;
        }

        public List<ArraySegment<byte>> SerializeRemoteException(RemoteException2 remoteException)
        {
            var serializer = new DataContractSerializer(
                typeof(RemoteException2),
                new DataContractSerializerSettings()
                {
                    MaxItemsInObjectGraph = int.MaxValue,
                });

            using (var stream = new MemoryStream())
            {
                using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
                {
                    serializer.WriteObject(writer, remoteException);
                    writer.Flush();
                    return new List<ArraySegment<byte>>()
                    {
                       new ArraySegment<byte>(stream.ToArray()),
                    };
                }
            }
        }

        public List<ArraySegment<byte>> SerializeRemoteException(Exception exception)
        {
            /*
             * If both BinaryFormatter and DCS(compat) are allowed
             *  - On the service side, serialize with BinaryFormatter to not break old clients
             *  - On the client side, attempt DCS first, then BinaryFormatter if DCS fails.
             */

            if (this.listenerSettings.AllowedExceptionSerializationMethods.HasFlag(ExceptionSerializationOptions.BinaryFormatter))
            {
                return RemoteException.FromException(exception).Data;
            }
            else
            {
                var svcEx = this.ToServiceException(exception);
                var remoteEx = this.ToRemoteException(svcEx);

                return this.SerializeRemoteException(remoteEx);
            }
        }

        public class DefaultExceptionConvetor : IExceptionConvertor
        {
            public Exception[] GetInnerExceptions(Exception exception)
            {
               return exception.InnerException == null ? null : new Exception[] { exception.InnerException };
            }

            public bool TryConvertToServiceException(Exception originalException, out ServiceException serviceException)
            {
                serviceException = new ServiceException(originalException.GetType().ToString(), originalException.Message);
                serviceException.ActualExceptionStackTrace = originalException.StackTrace;
                serviceException.ActualExceptionData = new Dictionary<string, string>()
                {
                    { "HResult", originalException.HResult.ToString() },
                };

                if (originalException is FabricException fabricEx)
                {
                    serviceException.ActualExceptionData.Add("FabricErrorCode", ((long)fabricEx.ErrorCode).ToString());
                }

                return true;
            }
        }
    }
}
