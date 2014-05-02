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

using Microsoft.Data.Entity.Utilities;
using Xunit;

namespace Microsoft.Data.Entity.Tests.Utilities
{
    public class LazyRefTest
    {
        [Fact]
        public void Has_value_is_false_until_value_accessed()
        {
            var lazy = new LazyRef<string>(() => "Cherry Coke");

            Assert.False(lazy.HasValue);
            Assert.Equal("Cherry Coke", lazy.Value);
            Assert.True(lazy.HasValue);
        }

        [Fact]
        public void Value_can_be_set_explicitly()
        {
            var lazy = new LazyRef<string>(() => "Cherry Coke");

            lazy.Value = "Fresca";

            Assert.True(lazy.HasValue);
            Assert.Equal("Fresca", lazy.Value);
        }

        [Fact]
        public void Initialization_can_be_reset()
        {
            var lazy = new LazyRef<string>(() => "Cherry Coke");

            Assert.Equal("Cherry Coke", lazy.Value);

            lazy.Reset(() => "Fresca");

            Assert.False(lazy.HasValue);
            Assert.Equal("Fresca", lazy.Value);
            Assert.True(lazy.HasValue);
        }
    }
}
