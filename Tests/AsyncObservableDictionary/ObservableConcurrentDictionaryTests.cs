// Copyright (c) FluentInjections Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Async.Collections;

using Castle.Core.Logging;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Async.Collections.Tests.AsyncObservableDictionary
{
    public class ObservableConcurrentDictionaryTests
    {
        private readonly ILogger<AsyncObservableDictionary<int, string>> _logger;

        public ObservableConcurrentDictionaryTests()
        {
            _logger = NullLogger<AsyncObservableDictionary<int, string>>.Instance;
        }

        [Fact]
        public void TryAdd_ShouldAddItemAndNotifyObservers()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var value = "testValue";
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Add && change.Key.Equals(key) && change.Value.Equals(value))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            var result = dictionary.TryAdd(key, value);

            // Assert
            result.Should().BeTrue();
            dictionary.Should().ContainKey(key);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void TryUpdate_ShouldUpdateItemAndNotifyObservers()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var oldValue = "oldValue";
            var newValue = "newValue";
            dictionary.TryAdd(key, oldValue);
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Update && change.Key.Equals(key) && change.Value.Equals(newValue))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            var result = dictionary.TryUpdate(key, newValue, oldValue);

            // Assert
            result.Should().BeTrue();
            dictionary.Should().ContainKey(key).WhoseValue.Should().Be(newValue);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void TryRemove_ShouldRemoveItemAndNotifyObservers()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var value = "testValue";
            dictionary.TryAdd(key, value);
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Remove && change.Key.Equals(key) && change.Value.Equals(value))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            var result = dictionary.TryRemove(key, out var removedValue);

            // Assert
            result.Should().BeTrue();
            removedValue.Should().Be(value);
            dictionary.Should().NotContainKey(key);
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Subscribe_ShouldNotifyObserversOnAdd()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var value = "testValue";
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Add && change.Key.Equals(key) && change.Value.Equals(value))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            dictionary.TryAdd(key, value);

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Subscribe_ShouldNotifyObserversOnUpdate()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var oldValue = "oldValue";
            var newValue = "newValue";
            dictionary.TryAdd(key, oldValue);
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Update && change.Key.Equals(key) && change.Value.Equals(newValue))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            dictionary[key] = newValue;

            // Assert
            eventRaised.Should().BeTrue();
        }

        [Fact]
        public void Subscribe_ShouldNotifyObserversOnRemove()
        {
            // Arrange
            var dictionary = new AsyncObservableDictionary<int, string>(_logger);
            var key = 1;
            var value = "testValue";
            dictionary.TryAdd(key, value);
            var eventRaised = false;

            dictionary.Subscribe(change =>
            {
                if (change.ChangeType == CollectionChangeType.Remove && change.Key.Equals(key) && change.Value.Equals(value))
                {
                    eventRaised = true;
                }
                return ValueTask.CompletedTask;
            });

            // Act
            dictionary.TryRemove(key, out _);

            // Assert
            eventRaised.Should().BeTrue();
        }
    }
}
