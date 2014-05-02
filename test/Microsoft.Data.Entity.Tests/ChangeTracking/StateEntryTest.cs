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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Advanced;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Identity;
using Microsoft.Data.Entity.Metadata;
using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Tests.ChangeTracking
{
    public class StateEntryTest
    {
        [Fact]
        public void Changing_state_from_Unknown_causes_entity_to_start_tracking()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");

            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());
            entry[keyProperty] = 1;

            entry.EntityState = EntityState.Added;

            Assert.Equal(EntityState.Added, entry.EntityState);
            Assert.Contains(entry, configuration.Services.StateManager.StateEntries);
        }

        [Fact]
        public void Changing_state_to_Unknown_causes_entity_to_stop_tracking()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");

            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());
            entry[keyProperty] = 1;

            entry.EntityState = EntityState.Added;
            entry.EntityState = EntityState.Unknown;

            Assert.Equal(EntityState.Unknown, entry.EntityState);
            Assert.DoesNotContain(entry, configuration.Services.StateManager.StateEntries);
        }

        [Fact]
        public void Changing_state_to_Modified_or_Unchanged_causes_all_properties_to_be_marked_accordingly()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var nonKeyProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());
            entry[keyProperty] = 1;

            Assert.False(entry.IsPropertyModified(keyProperty));
            Assert.False(entry.IsPropertyModified(nonKeyProperty));

            entry.EntityState = EntityState.Modified;

            Assert.True(entry.IsPropertyModified(keyProperty));
            Assert.True(entry.IsPropertyModified(nonKeyProperty));

            entry.EntityState = EntityState.Unchanged;

            Assert.False(entry.IsPropertyModified(keyProperty));
            Assert.False(entry.IsPropertyModified(nonKeyProperty));
        }

        [Fact]
        public void Changing_state_to_Added_triggers_key_generation()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");

            var generatorMock = new Mock<IIdentityGenerator>();
            generatorMock.Setup(m => m.NextAsync(CancellationToken.None)).Returns(Task.FromResult((object)77));

            var generatorFactory = new Mock<IdentityGeneratorFactory>();
            generatorFactory.Setup(m => m.Create(keyProperty)).Returns(generatorMock.Object);

            var serviceProvider = new ServiceCollection()
                .AddEntityFramework(s => s.UseIdentityGeneratorFactory(generatorFactory.Object))
                .BuildServiceProvider();

            var configuration = TestHelpers.CreateContextConfiguration(serviceProvider, model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());

            entry.EntityState = EntityState.Added;

            Assert.Equal(77, entry[keyProperty]);
        }

        [Fact]
        public void Can_create_primary_key()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());
            entry[keyProperty] = 77;

            var keyValue = entry.GetPrimaryKeyValue();
            Assert.IsType<SimpleEntityKey<int>>(keyValue);
            Assert.Equal(77, keyValue.Value);
        }

        [Fact]
        public void Can_create_foreign_key_value_based_on_dependent_values()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeDependentEntity");
            var fkProperty = entityType.GetProperty("SomeEntityId");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeDependentEntity());
            entry[fkProperty] = 77;

            var keyValue = entry.GetDependentKeyValue(entityType.ForeignKeys.Single());
            Assert.IsType<SimpleEntityKey<int>>(keyValue);
            Assert.Equal(77, keyValue.Value);
        }

        [Fact]
        public void Can_create_foreign_key_value_based_on_principal_end_values()
        {
            var model = BuildModel();
            var principalType = model.GetEntityType("SomeEntity");
            var dependentType = model.GetEntityType("SomeDependentEntity");
            var key = principalType.GetProperty("Id");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, principalType, new SomeEntity());
            entry[key] = 77;

            var keyValue = entry.GetPrincipalKeyValue(dependentType.ForeignKeys.Single());
            Assert.IsType<SimpleEntityKey<int>>(keyValue);
            Assert.Equal(77, keyValue.Value);
        }

        [Fact]
        public void Can_get_property_value_after_creation_from_value_buffer()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            Assert.Equal(1, entry[keyProperty]);
        }

        [Fact]
        public void Can_set_property_value_after_creation_from_value_buffer()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            entry[keyProperty] = 77;

            Assert.Equal(77, entry[keyProperty]);
        }

        [Fact]
        public void Can_set_and_get_property_values()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var nonKeyProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());

            entry[keyProperty] = 77;
            entry[nonKeyProperty] = "Magic Tree House";

            Assert.Equal(77, entry[keyProperty]);
            Assert.Equal("Magic Tree House", entry[nonKeyProperty]);
        }

        [Fact]
        public void Can_get_value_buffer_from_properties()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var keyProperty = entityType.GetProperty("Id");
            var nonKeyProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new SomeEntity());

            entry[keyProperty] = 77;
            entry[nonKeyProperty] = "Magic Tree House";

            Assert.Equal(new object[] { 77, "Magic Tree House" }, entry.GetValueBuffer());
        }

        [Fact]
        public void All_original_values_can_be_accessed_for_entity_that_does_full_change_tracking_if_eager_values_on()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("FullNotificationEntity");
            entityType.UseLazyOriginalValues = false;

            AllOriginalValuesTest(model, entityType);
        }

        protected void AllOriginalValuesTest(IModel model, IEntityType entityType)
        {
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            Assert.Equal(1, entry.OriginalValues[idProperty]);
            Assert.Equal("Kool", entry.OriginalValues[nameProperty]);
            Assert.Equal(1, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry[idProperty] = 2;
            entry[nameProperty] = "Beans";

            Assert.Equal(1, entry.OriginalValues[idProperty]);
            Assert.Equal("Kool", entry.OriginalValues[nameProperty]);
            Assert.Equal(2, entry[idProperty]);
            Assert.Equal("Beans", entry[nameProperty]);

            entry.OriginalValues[idProperty] = 3;
            entry.OriginalValues[nameProperty] = "Franks";

            Assert.Equal(3, entry.OriginalValues[idProperty]);
            Assert.Equal("Franks", entry.OriginalValues[nameProperty]);
            Assert.Equal(2, entry[idProperty]);
            Assert.Equal("Beans", entry[nameProperty]);
        }

        [Fact]
        public void Required_original_values_can_be_accessed_for_entity_that_does_full_change_tracking()
        {
            var model = BuildModel();
            OriginalValuesTest(model, model.GetEntityType("FullNotificationEntity"));
        }

        [Fact]
        public void Required_original_values_can_be_accessed_for_entity_that_does_changed_only_notification()
        {
            var model = BuildModel();
            OriginalValuesTest(model, model.GetEntityType("ChangedOnlyEntity"));
        }

        [Fact]
        public void Required_original_values_can_be_accessed_for_entity_that_does_no_notification()
        {
            var model = BuildModel();
            OriginalValuesTest(model, model.GetEntityType("SomeEntity"));
        }

        protected void OriginalValuesTest(IModel model, IEntityType entityType)
        {
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            Assert.Equal("Kool", entry.OriginalValues[nameProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry[nameProperty] = "Beans";

            Assert.Equal("Kool", entry.OriginalValues[nameProperty]);
            Assert.Equal("Beans", entry[nameProperty]);

            entry.OriginalValues[nameProperty] = "Franks";

            Assert.Equal("Franks", entry.OriginalValues[nameProperty]);
            Assert.Equal("Beans", entry[nameProperty]);
        }

        [Fact]
        public void Null_original_values_are_handled_for_entity_that_does_full_change_tracking()
        {
            var model = BuildModel();
            NullOriginalValuesTest(model, model.GetEntityType("FullNotificationEntity"));
        }

        [Fact]
        public void Null_original_values_are_handled_for_entity_that_does_changed_only_notification()
        {
            var model = BuildModel();
            NullOriginalValuesTest(model, model.GetEntityType("ChangedOnlyEntity"));
        }

        [Fact]
        public void Null_original_values_are_handled_for_entity_that_does_no_notification()
        {
            var model = BuildModel();
            NullOriginalValuesTest(model, model.GetEntityType("SomeEntity"));
        }

        protected void NullOriginalValuesTest(IModel model, IEntityType entityType)
        {
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, null }));

            Assert.Null(entry.OriginalValues[nameProperty]);
            Assert.Null(entry[nameProperty]);

            entry[nameProperty] = "Beans";

            Assert.Null(entry.OriginalValues[nameProperty]);
            Assert.Equal("Beans", entry[nameProperty]);

            entry.OriginalValues[nameProperty] = "Franks";

            Assert.Equal("Franks", entry.OriginalValues[nameProperty]);
            Assert.Equal("Beans", entry[nameProperty]);

            entry.OriginalValues[nameProperty] = null;

            Assert.Null(entry.OriginalValues[nameProperty]);
            Assert.Equal("Beans", entry[nameProperty]);
        }

        [Fact]
        public void Setting_property_using_state_entry_always_marks_as_modified()
        {
            var model = BuildModel();

            SetPropertyStateEntryTest(model, model.GetEntityType("FullNotificationEntity"));
            SetPropertyStateEntryTest(model, model.GetEntityType("ChangedOnlyEntity"));
            SetPropertyStateEntryTest(model, model.GetEntityType("SomeEntity"));
        }

        protected void SetPropertyStateEntryTest(IModel model, IEntityType entityType)
        {
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = EntityState.Unchanged;

            Assert.False(entry.IsPropertyModified(idProperty));
            Assert.False(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Unchanged, entry.EntityState);

            entry[idProperty] = 1;
            entry[nameProperty] = "Kool";

            Assert.False(entry.IsPropertyModified(idProperty));
            Assert.False(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Unchanged, entry.EntityState);

            entry[idProperty] = 2;
            entry[nameProperty] = "Beans";

            Assert.True(entry.IsPropertyModified(idProperty));
            Assert.True(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Modified, entry.EntityState);
        }

        protected void SetPropertyClrTest<TEntity>(bool needsDetectChanges)
            where TEntity : ISomeEntity
        {
            var model = BuildModel();
            var entityType = model.GetEntityType(typeof(TEntity));
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = EntityState.Unchanged;

            var entity = (TEntity)entry.Entity;

            Assert.False(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Unchanged, entry.EntityState);

            entity.Name = "Kool";

            Assert.False(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Unchanged, entry.EntityState);

            entity.Name = "Beans";

            if (needsDetectChanges)
            {
                Assert.False(entry.IsPropertyModified(nameProperty));
                Assert.Equal(EntityState.Unchanged, entry.EntityState);

                entry.DetectChanges();
            }

            Assert.True(entry.IsPropertyModified(nameProperty));
            Assert.Equal(EntityState.Modified, entry.EntityState);
        }

        [Fact]
        public void AcceptChanges_does_nothing_for_unchanged_entities()
        {
            AcceptChangesNoop(EntityState.Unchanged);
        }

        [Fact]
        public void AcceptChanges_does_nothing_for_unknown_entities()
        {
            AcceptChangesNoop(EntityState.Unknown);
        }

        private void AcceptChangesNoop(EntityState entityState)
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = entityState;

            entry.AcceptChanges();

            Assert.Equal(entityState, entry.EntityState);
        }

        [Fact]
        public void AcceptChanges_makes_Modified_entities_Unchanged_and_resets_used_original_values()
        {
            AcceptChangesKeep(EntityState.Modified);
        }

        [Fact]
        public void AcceptChanges_makes_Added_entities_Unchanged()
        {
            AcceptChangesKeep(EntityState.Added);
        }

        private void AcceptChangesKeep(EntityState entityState)
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = entityState;

            entry[nameProperty] = "Pickle";
            entry.OriginalValues[nameProperty] = "Cheese";

            entry.AcceptChanges();

            Assert.Equal(EntityState.Unchanged, entry.EntityState);
            Assert.Equal("Pickle", entry[nameProperty]);
            Assert.Equal("Pickle", entry.OriginalValues[nameProperty]);
        }

        [Fact]
        public void AcceptChanges_makes_Modified_entities_Unchanged_and_effectively_resets_unused_original_values()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var nameProperty = entityType.GetProperty("Name");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = EntityState.Modified;

            entry[nameProperty] = "Pickle";

            entry.AcceptChanges();

            Assert.Equal(EntityState.Unchanged, entry.EntityState);
            Assert.Equal("Pickle", entry[nameProperty]);
            Assert.Equal("Pickle", entry.OriginalValues[nameProperty]);
        }

        [Fact]
        public void AcceptChanges_detaches_Deleted_entities()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var configuration = TestHelpers.CreateContextConfiguration(model);

            var entry = CreateStateEntry(configuration, entityType, new ObjectArrayValueReader(new object[] { 1, "Kool" }));
            entry.EntityState = EntityState.Deleted;

            entry.AcceptChanges();

            Assert.Equal(EntityState.Unknown, entry.EntityState);
        }

        [Fact]
        public void Can_add_and_remove_sidecars()
        {
            var model = BuildModel();
            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                model.GetEntityType("SomeEntity"),
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecarMock1 = new Mock<Sidecar>();
            sidecarMock1.Setup(m => m.Name).Returns("IMZ-Ural");
            sidecarMock1.Setup(m => m.AutoCommit).Returns(true);

            var sidecarMock2 = new Mock<Sidecar>();
            sidecarMock2.Setup(m => m.Name).Returns("GG Duetto");

            var originalValues = entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues);

            Assert.True(entry.EntityType.HasClrType == (originalValues != null));
            Assert.Null(entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));

            entry.AddSidecar(sidecarMock1.Object);
            entry.AddSidecar(sidecarMock2.Object);

            Assert.Same(originalValues, entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Same(sidecarMock1.Object, entry.TryGetSidecar("IMZ-Ural"));
            Assert.Same(sidecarMock2.Object, entry.TryGetSidecar("GG Duetto"));

            entry.RemoveSidecar("IMZ-Ural");
            entry.RemoveSidecar("GG Duetto");

            Assert.Same(originalValues, entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Null(entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));

            entry.RemoveSidecar("IMZ-Ural");
            entry.RemoveSidecar("GG Duetto");

            Assert.Same(originalValues, entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Null(entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));

            entry.RemoveSidecar(Sidecar.WellKnownNames.OriginalValues);

            Assert.Null(entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Null(entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));

            entry.RemoveSidecar("IMZ-Ural");
            entry.RemoveSidecar("GG Duetto");

            Assert.Null(entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Null(entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));

            entry.AddSidecar(sidecarMock1.Object);

            Assert.Null(entry.TryGetSidecar(Sidecar.WellKnownNames.OriginalValues));
            Assert.Same(sidecarMock1.Object, entry.TryGetSidecar("IMZ-Ural"));
            Assert.Null(entry.TryGetSidecar("GG Duetto"));
        }

        [Fact]
        public void Non_transparent_sidecar_does_not_intercept_normal_property_read_and_write()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecar = entry.AddSidecar(new TheWasp(entry, new[] { idProperty }));

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            sidecar[idProperty] = 7;

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry[idProperty] = 77;

            Assert.Equal(77, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);
        }

        [Fact]
        public void Can_read_values_from_sidecar_transparently()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecar = entry.AddSidecar(new TheWasp(entry, new[] { idProperty }, transparentRead: true));

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal(1, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            sidecar[idProperty] = 7;

            Assert.Equal(7, entry[idProperty]);
            Assert.Equal(7, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry[idProperty] = 77;

            Assert.Equal(7, entry[idProperty]);
            Assert.Equal(7, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry.RemoveSidecar(sidecar.Name);

            Assert.Equal(77, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);
        }

        [Fact]
        public void Can_write_values_to_sidecar_transparently()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecar = entry.AddSidecar(new TheWasp(entry, new[] { idProperty }, transparentWrite: true));

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry[idProperty] = 7;

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal(7, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            sidecar[idProperty] = 77;

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal(77, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry.RemoveSidecar(sidecar.Name);

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);
        }

        [Fact]
        public void Can_auto_commit_sidecars()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecar = entry.AddSidecar(new TheWasp(entry, new[] { idProperty }, autoCommit: true));

            sidecar[idProperty] = 77;

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal(77, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry.AutoCommitSidecars();

            Assert.Equal(77, entry[idProperty]);

            Assert.Null(entry.TryGetSidecar(sidecar.Name));
        }

        [Fact]
        public void Can_auto_rollback_sidecars()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            var sidecar = entry.AddSidecar(new TheWasp(entry, new[] { idProperty }, autoCommit: true));

            sidecar[idProperty] = 77;

            Assert.Equal(1, entry[idProperty]);
            Assert.Equal(77, sidecar[idProperty]);
            Assert.Equal("Kool", entry[nameProperty]);

            entry.AutoRollbackSidecars();

            Assert.Equal(1, entry[idProperty]);

            Assert.Null(entry.TryGetSidecar(sidecar.Name));
        }

        private class TheWasp : DictionarySidecar
        {
            private readonly bool _transparentRead;
            private readonly bool _transparentWrite;
            private readonly bool _autoCommit;

            public TheWasp(
                StateEntry stateEntry, IEnumerable<IProperty> properties,
                bool transparentRead = false, bool transparentWrite = false, bool autoCommit = false)
                : base(stateEntry, properties)
            {
                _transparentRead = transparentRead;
                _transparentWrite = transparentWrite;
                _autoCommit = autoCommit;
            }

            public override string Name
            {
                get { return "Wasp Motorcycles"; }
            }

            public override bool TransparentRead
            {
                get { return _transparentRead; }
            }

            public override bool TransparentWrite
            {
                get { return _transparentWrite; }
            }

            public override bool AutoCommit
            {
                get { return _autoCommit; }
            }
        }

        [Fact]
        public void Sidecars_are_added_for_store_generated_values_when_preparing_to_save()
        {
            var model = BuildModel();
            var entityType = model.GetEntityType("SomeEntity");
            var idProperty = entityType.GetProperty("Id");
            var nameProperty = entityType.GetProperty("Name");

            var entry = CreateStateEntry(
                TestHelpers.CreateContextConfiguration(model),
                entityType,
                new ObjectArrayValueReader(new object[] { 1, "Kool" }));

            entry.PrepareToSave();

            var storeGenValues = entry.TryGetSidecar(Sidecar.WellKnownNames.StoreGeneratedValues);

            Assert.NotNull(storeGenValues);
            Assert.True(storeGenValues.CanStoreValue(idProperty));
            Assert.False(storeGenValues.CanStoreValue(nameProperty));
        }

        protected virtual StateEntry CreateStateEntry(ContextConfiguration configuration, IEntityType entityType, object entity)
        {
            return new StateEntrySubscriber().SnapshotAndSubscribe(
                new StateEntryFactory(
                    configuration,
                    configuration.Services.ServiceProvider.GetRequiredService<EntityMaterializerSource>())
                    .Create(entityType, entity));
        }

        protected virtual StateEntry CreateStateEntry(ContextConfiguration configuration, IEntityType entityType, IValueReader valueReader)
        {
            return new StateEntrySubscriber().SnapshotAndSubscribe(
                new StateEntryFactory(
                    configuration,
                    configuration.Services.ServiceProvider.GetRequiredService<EntityMaterializerSource>())
                    .Create(entityType, valueReader));
        }

        protected virtual Model BuildModel()
        {
            var model = new Model();

            var entityType1 = new EntityType(typeof(SomeEntity));
            model.AddEntityType(entityType1);
            var key1 = entityType1.AddProperty("Id", typeof(int));
            key1.ValueGenerationStrategy = ValueGenerationStrategy.StoreIdentity;
            entityType1.SetKey(key1);
            entityType1.AddProperty("Name", typeof(string), shadowProperty: false, concurrencyToken: true);

            var entityType2 = new EntityType(typeof(SomeDependentEntity));
            model.AddEntityType(entityType2);
            var key2 = entityType2.AddProperty("Id", typeof(int));
            entityType2.SetKey(key2);
            var fk = entityType2.AddProperty("SomeEntityId", typeof(int));
            entityType2.AddForeignKey(entityType1.GetKey(), new[] { fk });

            var entityType3 = new EntityType(typeof(FullNotificationEntity));
            model.AddEntityType(entityType3);
            entityType3.SetKey(entityType3.AddProperty("Id", typeof(int)));
            entityType3.AddProperty("Name", typeof(string), shadowProperty: false, concurrencyToken: true);

            var entityType4 = new EntityType(typeof(ChangedOnlyEntity));
            model.AddEntityType(entityType4);
            entityType4.SetKey(entityType4.AddProperty("Id", typeof(int)));
            entityType4.AddProperty("Name", typeof(string), shadowProperty: false, concurrencyToken: true);

            return model;
        }

        protected interface ISomeEntity
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        protected class SomeEntity : ISomeEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        protected class SomeDependentEntity
        {
            public int Id { get; set; }
            public int SomeEntityId { get; set; }
        }

        protected class FullNotificationEntity : INotifyPropertyChanging, INotifyPropertyChanged, ISomeEntity
        {
            private int _id;
            private string _name;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        NotifyChanging();
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        NotifyChanging();
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangingEventHandler PropertyChanging;
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            private void NotifyChanging([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanging != null)
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
            }
        }

        protected class ChangedOnlyEntity : INotifyPropertyChanged, ISomeEntity
        {
            private int _id;
            private string _name;

            public int Id
            {
                get { return _id; }
                set
                {
                    if (_id != value)
                    {
                        _id = value;
                        NotifyChanged();
                    }
                }
            }

            public string Name
            {
                get { return _name; }
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        NotifyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyChanged([CallerMemberName] String propertyName = "")
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
