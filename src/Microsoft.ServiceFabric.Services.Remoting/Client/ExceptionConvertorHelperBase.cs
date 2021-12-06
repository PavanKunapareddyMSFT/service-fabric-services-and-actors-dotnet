// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.Client
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;

    internal class ExceptionConvertorHelperBase
    {
        private FabricTransportRemotingSettings remotingSettings;
        private IEnumerable<IExceptionConvertor> convertors;

        public ExceptionConvertorHelperBase(IEnumerable<IExceptionConvertor> convertors, FabricTransportRemotingSettings remotingSettings)
        {
            this.convertors = convertors;
            this.remotingSettings = remotingSettings;
        }

        protected FabricTransportRemotingSettings RemotingSettings
        {
            get
            {
                return this.remotingSettings;
            }
        }

        public Exception FromServiceException(ServiceException serviceException)
        {
            List<Exception> innerExceptions = new List<Exception>();
            if (serviceException.ActualInnerExceptions != null && serviceException.ActualInnerExceptions.Count > 0)
            {
                foreach (var inner in serviceException.ActualInnerExceptions)
                {
                    innerExceptions.Add(this.FromServiceException(inner));
                }
            }

            Exception actualEx = null;
            foreach (var convertor in this.convertors)
            {
                try
                {
                    if (innerExceptions.Count == 0)
                    {
                        if (convertor.TryConvertFromServiceException(serviceException, out actualEx))
                        {
                            break;
                        }
                    }
                    else if (innerExceptions.Count == 1)
                    {
                        if (convertor.TryConvertFromServiceException(serviceException, innerExceptions[0], out actualEx))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (convertor.TryConvertFromServiceException(serviceException, innerExceptions.ToArray(), out actualEx))
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Throw
                }
            }

            return actualEx != null ? actualEx : serviceException;
        }

        public ServiceException FromRemoteException(RemoteException2 remoteEx)
        {
            var svcEx = new ServiceException(remoteEx.Type, remoteEx.Message);
            svcEx.ActualExceptionStackTrace = remoteEx.StackTrace;
            svcEx.ActualExceptionData = remoteEx.Data;

            if (remoteEx.InnerExceptions != null && remoteEx.InnerExceptions.Count > 0)
            {
                svcEx.ActualInnerExceptions = new List<ServiceException>();
                foreach (var inner in remoteEx.InnerExceptions)
                {
                    svcEx.ActualInnerExceptions.Add(this.FromRemoteException(inner));
                }
            }

            return svcEx;
        }
    }
}
