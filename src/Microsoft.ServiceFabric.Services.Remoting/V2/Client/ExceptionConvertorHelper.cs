// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.V2.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
    using Microsoft.ServiceFabric.Services.Remoting.V2;

    internal class ExceptionConvertorHelper : ExceptionConvertorHelperBase
    {
        public ExceptionConvertorHelper(IEnumerable<IExceptionConvertor> convertors, FabricTransportRemotingSettings remotingSettings)
            : base(convertors, remotingSettings)
        {
        }

        public bool TryDeserializeRemoteException(Stream stream, out RemoteException2 remoteException)
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
                stream.Seek(0, SeekOrigin.Begin);
                using (var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    remoteException = (RemoteException2)serializer.ReadObject(reader);

                    return true;
                }
            }
            catch (Exception)
            {
                // Throw
            }

            return false;
        }

        public bool TryDeserializeRemoteException(Stream stream, out Exception exception)
        {
            exception = null;
            if (this.RemotingSettings.AllowedExceptionSerializationMethods.HasFlag(ExceptionSerializationOptions.DataContract))
            {
                if (this.TryDeserializeRemoteException(stream, out RemoteException2 remoteException))
                {
                    var svcEx = this.FromRemoteException(remoteException);
                    exception = this.FromServiceException(svcEx);

                    return true;
                }
            }
            else
            {
                return RemoteException.ToException(stream, out exception);
            }

            return false;
        }
    }
}
