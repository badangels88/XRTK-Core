﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Interfaces;

namespace XRTK.Services
{
    /// <summary>
    /// The base data provider implements <see cref="IMixedRealityDataProvider"/> and provides default properties for all data providers.
    /// </summary>
    /// <remarks>
    /// Empty, but reserved for future use, in case additional <see cref="IMixedRealityDataProvider"/> properties or methods are assigned.
    /// </remarks>
    public abstract class BaseDataProvider : BaseServiceWithConstructor, IMixedRealityDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        public BaseDataProvider(string name, uint priority) : base(name, priority) { }
    }
}