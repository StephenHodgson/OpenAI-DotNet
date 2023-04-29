﻿using NUnit.Framework;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OpenAI.Tests
{
    internal class TestFixture_03_Chat : AbstractTestFixture
    {
        [Test]
        public async Task Test_1_GetChatCompletion()
        {
            Assert.IsNotNull(OpenAIClient.ChatEndpoint);
            var messages = new List<Message>
            {
                new Message(Role.System, "You are a helpful assistant."),
                new Message(Role.User, "Who won the world series in 2020?"),
                new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
                new Message(Role.User, "Where was it played?"),
            };
            var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
            var result = await OpenAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);
            Assert.IsNotNull(result);
            Assert.NotNull(result.Choices);
            Assert.NotZero(result.Choices.Count);
            Console.WriteLine(result.FirstChoice);
        }

        [Test]
        public async Task Test_2_GetChatStreamingCompletion()
        {
            Assert.IsNotNull(OpenAIClient.ChatEndpoint);
            var messages = new List<Message>
            {
                new Message(Role.System, "You are a helpful assistant."),
                new Message(Role.User, "Who won the world series in 2020?"),
                new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
                new Message(Role.User, "Where was it played?"),
            };
            var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
            var finalResult = await OpenAIClient.ChatEndpoint.StreamCompletionAsync(chatRequest, result =>
             {
                 Assert.IsNotNull(result);
                 Assert.NotNull(result.Choices);
                 Assert.NotZero(result.Choices.Count);

                 foreach (var choice in result.Choices.Where(choice => choice.Delta?.Content != null))
                 {
                     Console.WriteLine($"{choice.Index}: {choice.Delta.Content}");
                 }

                 foreach (var choice in result.Choices.Where(choice => choice.Message?.Content != null))
                 {
                     Console.WriteLine($"{choice.Index}: {choice.Message.Content}");
                 }
             });

            Assert.IsNotNull(finalResult);
        }

        [Test]
        public async Task Test_3_GetChatStreamingCompletionEnumerableAsync()
        {
            Assert.IsNotNull(OpenAIClient.ChatEndpoint);
            var messages = new List<Message>
            {
                new Message(Role.System, "You are a helpful assistant."),
                new Message(Role.User, "Who won the world series in 2020?"),
                new Message(Role.Assistant, "The Los Angeles Dodgers won the World Series in 2020."),
                new Message(Role.User, "Where was it played?"),
            };
            var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
            await foreach (var result in OpenAIClient.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest))
            {
                Assert.IsNotNull(result);
                Assert.NotNull(result.Choices);
                Assert.NotZero(result.Choices.Count);

                foreach (var choice in result.Choices.Where(choice => choice.Delta?.Content != null))
                {
                    Console.WriteLine($"{choice.Index}: {choice.Delta.Content}");
                }

                foreach (var choice in result.Choices.Where(choice => choice.Message?.Content != null))
                {
                    Console.WriteLine($"{choice.Index}: {choice.Message.Content}");
                }
            }
        }
    }
}