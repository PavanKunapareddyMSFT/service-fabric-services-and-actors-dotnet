// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Services.Remoting.FabricTransport
{
    using System;

    /// <summary>
    /// Allowed Exception serializers.
    /// </summary>
    [Flags]
    public enum ExceptionSerializationOptions
    {
        /// <summary>
        /// Binary serialization using BinaryFormatter.
        /// </summary>
        BinaryFormatter,

        /// <summary>
        /// Xml serialization using DataContract.
        /// </summary>
        DataContract,
    }
}
}
