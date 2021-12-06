// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V1.Client
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;
    using Microsoft.ServiceFabric.FabricTransport;
    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
    using Microsoft.ServiceFabric.Services.Remoting.V1;

    internal class ExceptionConvertorHelper : ExceptionConvertorHelperBase
    {
        public ExceptionConvertorHelper(IEnumerable<IExceptionConvertor> convertors, FabricTransportRemotingSettings remotingSettings)
            : base(convertors, remotingSettings)
        {
        }

        public bool TryDeserializeRemoteException(byte[] bytes, out RemoteException2 remoteException)
        {
            remoteException = null;
            var serializer = new DataContractSerializer(
                typeof(RemoteException2),
                new DataContractSerializerSettings()
                {
                    MaxItemsInObjectGraph = int.MaxValue,
                });

            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    using (var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        remoteException = (RemoteException2)serializer.ReadObject(reader);

                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Throw
            }

            return false;
        }

        public bool TryDeserializeRemoteException(byte[] bytes, out Exception exception)
        {
            exception = null;
            if (this.RemotingSettings.AllowedExceptionSerializationMethods.HasFlag(ExceptionSerializationOptions.DataContract))
            {
                if (this.TryDeserializeRemoteException(bytes, out RemoteException2 remoteException))
                {
                    var svcEx = this.FromRemoteException(remoteException);
                    exception = this.FromServiceException(svcEx);

                    return true;
                }
            }
            else
            {
                return RemoteExceptionInformation.ToException(
                                new RemoteExceptionInformation(bytes),
                                out exception);
            }

            return false;
        }
    }
}
