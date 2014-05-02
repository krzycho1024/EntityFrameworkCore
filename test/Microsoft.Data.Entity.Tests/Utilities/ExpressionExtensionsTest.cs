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
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.Data.Entity.Tests.Utilities
{
    public class ExpressionExtensionsTest
    {
        [Fact]
        public void Get_property_access_should_return_property_info_when_valid_property_access_expression()
        {
            Expression<Func<DateTime, int>> expression = d => d.Hour;

            var propertyInfo = expression.GetPropertyAccess();

            Assert.NotNull(propertyInfo);
            Assert.Equal("Hour", propertyInfo.Name);
        }

        [Fact]
        public void Get_property_access_should_throw_when_not_property_access()
        {
            Expression<Func<DateTime, int>> expression = d => 123;

            Assert.Contains(
                Strings.FormatInvalidPropertyExpression(expression),
                Assert.Throws<ArgumentException>(() => expression.GetPropertyAccess()).Message);
        }

        [Fact]
        public void Get_property_access_should_throw_when_not_property_access_on_the_provided_argument()
        {
            var closure = DateTime.Now;
            Expression<Func<DateTime, int>> expression = d => closure.Hour;

            Assert.Contains(
                Strings.FormatInvalidPropertyExpression(expression),
                Assert.Throws<ArgumentException>(() => expression.GetPropertyAccess()).Message);
        }

        [Fact]
        public void Get_property_access_should_remove_convert()
        {
            Expression<Func<DateTime, long>> expression = d => d.Hour;

            var propertyInfo = expression.GetPropertyAccess();

            Assert.NotNull(propertyInfo);
            Assert.Equal("Hour", propertyInfo.Name);
        }

        [Fact]
        public void Get_property_access_list_should_return_property_info_collection()
        {
            Expression<Func<DateTime, object>> expression = d => new
                {
                    d.Date,
                    d.Day
                };

            var propertyInfos = expression.GetPropertyAccessList();

            Assert.NotNull(propertyInfos);
            Assert.Equal(2, propertyInfos.Count);
            Assert.Equal("Date", propertyInfos.First().Name);
            Assert.Equal("Day", propertyInfos.Last().Name);
        }

        [Fact]
        public void Get_property_access_list_should_throw_when_invalid_expression()
        {
            Expression<Func<DateTime, object>> expression = d => new
                {
                    P = d.AddTicks(23)
                };

            Assert.Contains(
                Strings.FormatInvalidPropertiesExpression(expression),
                Assert.Throws<ArgumentException>(() => expression.GetPropertyAccessList()).Message);
        }

        [Fact]
        public void Get_property_access_list_should_throw_when_property_access_not_on_the_provided_argument()
        {
            var closure = DateTime.Now;

            Expression<Func<DateTime, object>> expression = d => new
                {
                    d.Date,
                    closure.Day
                };

            Assert.Contains(
                Strings.FormatInvalidPropertiesExpression(expression),
                Assert.Throws<ArgumentException>(() => expression.GetPropertyAccessList()).Message);
        }
    }
}
