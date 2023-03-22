using NUnit.Framework;
using OpenAI.FineTuning;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OpenAI.Tests
{
    internal sealed class TestFixture_08_Files : AbstractTestFixture
    {
        [Test]
        public async Task Test_01_UploadFileAsync()
        {
            Assert.IsNotNull(this.OpenAIClient.FilesEndpoint);
            await File.WriteAllTextAsync("test.jsonl", new FineTuningTrainingData("I'm a", "learning language model"));
            Assert.IsTrue(File.Exists("test.jsonl"));
            var result = await this.OpenAIClient.FilesEndpoint.UploadFileAsync("test.jsonl", "fine-tune");

            Assert.IsNotNull(result);
            Assert.IsTrue(result.FileName == "test.jsonl");
            Console.WriteLine($"{result.Id} -> {result.Object}");

            File.Delete("test.jsonl");
            Assert.IsFalse(File.Exists("test.jsonl"));
        }

        [Test]
        public async Task Test_02_ListFilesAsync()
        {
            Assert.IsNotNull(this.OpenAIClient.FilesEndpoint);
            var result = await this.OpenAIClient.FilesEndpoint.ListFilesAsync();

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);

            foreach (var file in result)
            {
                var fileInfo = await this.OpenAIClient.FilesEndpoint.GetFileInfoAsync(file);
                Assert.IsNotNull(fileInfo);
                Console.WriteLine($"{fileInfo.Id} -> {fileInfo.Object}: {fileInfo.FileName} | {fileInfo.Size} bytes");
            }
        }

        [Test]
        public async Task Test_03_DownloadFileAsync()
        {
            Assert.IsNotNull(this.OpenAIClient.FilesEndpoint);
            var files = await this.OpenAIClient.FilesEndpoint.ListFilesAsync();

            Assert.IsNotNull(files);
            Assert.IsNotEmpty(files);

            var testFileData = files[0];
            var result = await this.OpenAIClient.FilesEndpoint.DownloadFileAsync(testFileData, Directory.GetCurrentDirectory());

            Assert.IsNotNull(result);
            Console.WriteLine(result);
            Assert.IsTrue(File.Exists(result));

            File.Delete(result);
            Assert.IsFalse(File.Exists(result));
        }

        [Test]
        public async Task Test_04_DeleteFilesAsync()
        {
            Assert.IsNotNull(this.OpenAIClient.FilesEndpoint);
            var files = await this.OpenAIClient.FilesEndpoint.ListFilesAsync();
            Assert.IsNotNull(files);
            Assert.IsNotEmpty(files);

            foreach (var file in files)
            {
                var result = await this.OpenAIClient.FilesEndpoint.DeleteFileAsync(file);
                Assert.IsTrue(result);
                Console.WriteLine($"{file.Id} -> deleted");
            }

            files = await this.OpenAIClient.FilesEndpoint.ListFilesAsync();
            Assert.IsNotNull(files);
            Assert.IsEmpty(files);
        }
    }
}
