﻿namespace NServiceBus.AzureStoragePersistence.Tests.Timeouts
 {
     using System;
     using System.Collections.Generic;
     using System.Linq;
     using System.Threading.Tasks;
     using NServiceBus.Timeout.Core;
     using NUnit.Framework;

     [TestFixture]
     [Category("AzureStoragePersistence")]
     public class When_removing_timeouts_from_the_storage
     {
         [SetUp]
         public void Perform_storage_cleanup()
         {
             TestHelper.PerformStorageCleanup();
         }

         [Test]
         public void Should_return_correct_headers_when_timeout_is_TryRemoved()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();

             var timeout = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);

             Assert.True(timeouts.Count == 1);

             TimeoutData timeoutData;
             timeoutPersister.TryRemove(timeouts.First().Item1, out timeoutData);

             CollectionAssert.AreEqual(new Dictionary<string, string> { { "Prop1", "1234" }, { "Prop2", "text" } }, timeoutData.Headers);
         }

         [Test]
         public void Should_return_correct_headers_when_timeout_is_Peeked()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();

             var timeout = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);

             Assert.True(timeouts.Count == 1);

             var timeoutId = timeouts.First().Item1;
             var timeoutData = timeoutPersister.Peek(timeoutId);

             CollectionAssert.AreEqual(new Dictionary<string, string> { { "Prop1", "1234" }, { "Prop2", "text" } }, timeoutData.Headers);
         }

         [Test]
         public void Peek_should_return_null_for_non_existing_timeout()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();

             var timeoutData = timeoutPersister.Peek("A2B34534324F3435A324234C");

             Assert.IsNull(timeoutData);
         }

         [Test]
         public void Should_remove_timeouts_by_id_using_old_interface()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();
             var timeout1 = TestHelper.GenerateTimeoutWithHeaders();
             var timeout2 = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout1);
             timeoutPersister.Add(timeout2);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
             Assert.IsTrue(timeouts.Count == 2);

             foreach (var timeout in timeouts)
             {
                 TimeoutData timeoutData;
                 timeoutPersister.TryRemove(timeout.Item1, out timeoutData);
             }

             TestHelper.AssertAllTimeoutsThatHaveBeenRemoved(timeoutPersister);
         }

         [Test]
         public void Should_remove_timeouts_by_id_and_return_true_using_new_interface()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();
             var timeout1 = TestHelper.GenerateTimeoutWithHeaders();
             var timeout2 = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout1);
             timeoutPersister.Add(timeout2);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
             Assert.IsTrue(timeouts.Count == 2);

             var itemRemoved = true;
             foreach (var timeout in timeouts)
             {
                 itemRemoved &= timeoutPersister.TryRemove(timeout.Item1);
             }

             Assert.IsTrue(itemRemoved, "Expected 2 ivocations to return true, but one or both of them returned false");

             TestHelper.AssertAllTimeoutsThatHaveBeenRemoved(timeoutPersister);
         }

         [Test]
         public void Should_return_false_if_timeout_already_deleted_for_TryRemove_invocation()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();

             var timeout = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
             Assert.IsTrue(timeouts.Count == 1);

             var timeoutId = timeouts.First().Item1;

             Assert.IsTrue(timeoutPersister.TryRemove(timeoutId));
             Assert.IsFalse(timeoutPersister.TryRemove(timeoutId));
         }

         [Test]
         public void Should_remove_timeouts_by_sagaid()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();
             var sagaId1 = Guid.NewGuid();
             var sagaId2 = Guid.NewGuid();
             var timeout1 = TestHelper.GetnerateTimeoutWithSagaId(sagaId1);
             var timeout2 = TestHelper.GetnerateTimeoutWithSagaId(sagaId2);
             timeoutPersister.Add(timeout1);
             timeoutPersister.Add(timeout2);

             var timeouts = TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
             Assert.IsTrue(timeouts.Count == 2);

             timeoutPersister.RemoveTimeoutBy(sagaId1);
             timeoutPersister.RemoveTimeoutBy(sagaId2);

             TestHelper.AssertAllTimeoutsThatHaveBeenRemoved(timeoutPersister);
         }

         [Test]
         public async Task TryRemove_should_work_with_concurrent_operations()
         {
             var timeoutPersister = TestHelper.CreateTimeoutPersister();
             var timeout = TestHelper.GenerateTimeoutWithHeaders();
             timeoutPersister.Add(timeout);

             var task1 = Task.Run(() => timeoutPersister.TryRemove(timeout.Id));
             var task2 = Task.Run(() => timeoutPersister.TryRemove(timeout.Id));

             await Task.WhenAll(task1, task2).ConfigureAwait(false);

             Assert.IsTrue(task1.Result || task2.Result);
             Assert.IsFalse(task1.Result && task2.Result);
         }
     }
 }