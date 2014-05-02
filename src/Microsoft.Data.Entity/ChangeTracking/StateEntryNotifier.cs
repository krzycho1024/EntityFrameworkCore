// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.ChangeTracking
{
    public class StateEntryNotifier
    {
        private readonly IEntityStateListener[] _entityStateListeners;

        /// <summary>
        ///     This constructor is intended only for use when creating test doubles that will override members
        ///     with mocked or faked behavior. Use of this constructor for other purposes may result in unexpected
        ///     behavior including but not limited to throwing <see cref="NullReferenceException" />.
        /// </summary>
        protected StateEntryNotifier()
        {
        }

        public StateEntryNotifier([NotNull] IEnumerable<IEntityStateListener> entityStateListeners)
        {
            Check.NotNull(entityStateListeners, "entityStateListeners");

            var stateListeners = entityStateListeners.ToArray();
            _entityStateListeners = stateListeners.Length == 0 ? null : stateListeners;
        }

        public virtual void StateChanging([NotNull] StateEntry entry, EntityState newState)
        {
            Check.NotNull(entry, "entry");

            if (_entityStateListeners == null)
            {
                return;
            }

            foreach (var listener in _entityStateListeners)
            {
                listener.StateChanging(entry, newState);
            }
        }

        public virtual void StateChanged([NotNull] StateEntry entry, EntityState oldState)
        {
            Check.NotNull(entry, "entry");

            if (_entityStateListeners == null)
            {
                return;
            }

            foreach (var listener in _entityStateListeners)
            {
                listener.StateChanged(entry, oldState);
            }
        }
    }
}
