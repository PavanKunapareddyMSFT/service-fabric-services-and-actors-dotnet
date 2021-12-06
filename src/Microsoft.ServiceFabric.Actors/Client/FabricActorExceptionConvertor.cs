// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Client
{
    using System;
    using Microsoft.ServiceFabric.Services.Communication;
    using Microsoft.ServiceFabric.Services.Remoting.Client;

    internal class FabricActorExceptionConvertor : IExceptionConvertor
    {
        public bool TryConvertFromServiceException(ServiceException serviceException, out Exception actualException)
        {
            return this.TryConvertFromServiceException(serviceException, (Exception[])null, out actualException);
        }

        public bool TryConvertFromServiceException(ServiceException serviceException, Exception innerException, out Exception actualException)
        {
            return this.TryConvertFromServiceException(serviceException, new Exception[] { innerException }, out actualException);
        }

        public bool TryConvertFromServiceException(ServiceException serviceException, Exception[] innerExceptions, out Exception actualException)
        {
            actualException = null;
            if (FabricActorExceptionKnownTypes.ServiceExceptionConvertors.TryGetValue(serviceException.ActualExceptionType, out var func))
            {
                actualException = func.FromServiceExFunc(serviceException, innerExceptions);

                return true;
            }

            return false;
        }
    }
}
