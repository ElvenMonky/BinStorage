using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace BinStorage.PerfTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || !Directory.Exists(args[0]) || !Directory.Exists(args[1]))
            {
                Console.WriteLine("Usage: BinStorage.TestApp.exe InputFolder StorageFolder");
                return;
            }

            var infos = new Dictionary<string, StreamInfo>();
            var files = Directory.EnumerateFiles(args[0], "*", SearchOption.AllDirectories);

            Console.WriteLine();
            Console.WriteLine($"Calculating source data length and hash from {args[0]}");
            var elapsed1 = DoVerification(files, args[1], (storage, key) =>
            {
                ComputeFileInfo(infos, key);
            }, 1);
            Console.WriteLine($"Time to compute source hash (no storage used): {elapsed1}");
            Console.WriteLine();

            Console.WriteLine($"Clearing storage at {args[1]}");
            var storageFiles = Directory.EnumerateFiles(args[1], "*", SearchOption.AllDirectories);
            storageFiles.AsParallel().ForAll(x => File.Delete(x));
            Console.WriteLine();

            Console.WriteLine($"Creating and verifying storage from {args[0]}");
            var elapsed2 = DoVerification(files, args[1], (storage, key) =>
            {
                AddFile(storage, key);
                CheckFile(storage, key, infos[key]);
            });
            Console.WriteLine($"Time to create and verify: {elapsed2}");
            Console.WriteLine();

            Console.WriteLine($"Clearing storage at {args[1]}");
            storageFiles = Directory.EnumerateFiles(args[1], "*", SearchOption.AllDirectories);
            storageFiles.AsParallel().ForAll(x => File.Delete(x));
            Console.WriteLine();

            Console.WriteLine($"Creating storage from {args[0]}");
            var elapsed3 = DoVerification(files, args[1], AddFile);
            Console.WriteLine($"Time to create: {elapsed3}");
            Console.WriteLine();

            Console.WriteLine("Verifying data");
            var elapsed4 = DoVerification(files, args[1], (storage, key) =>
            {
                CheckFile(storage, key, infos[key]);
            });
            Console.WriteLine($"Time to verify: {elapsed4}");
            Console.WriteLine();

            Console.WriteLine("Summary:");
            Console.WriteLine($"Time to compute source hash (no storage used): {elapsed1}");
            Console.WriteLine($"Time to create and verify: {elapsed2}");
            Console.WriteLine($"Time to create: {elapsed3}");
            Console.WriteLine($"Time to verify: {elapsed4}");

            Console.ReadLine();
        }

        static TimeSpan DoVerification<T>(IEnumerable<T> items, string workingFolder, Action<IBinaryStorage, T> action, int threads = 4)
        {
            var count = items.Count();
            Stopwatch sw = Stopwatch.StartNew();
            using (var storage = new BinaryStorage(new StorageConfiguration() { WorkingFolder = workingFolder }))
            {
                var processed = 0;
                items.AsParallel().WithDegreeOfParallelism(threads).ForAll(x =>
                {
                    action(storage, x);

                    var progress = Interlocked.Increment(ref processed);
                    if (progress % 1002 == 0)
                    {
                        Console.WriteLine($"Progress: {100.0 * progress / count,5:0.00}%, time: {sw.Elapsed}");
                    }
                });
            }
            return sw.Elapsed;
        }

        static void ComputeFileInfo(Dictionary<string, StreamInfo> info, string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var md5 = MD5.Create())
                {
                    var hash1 = md5.ComputeHash(file);
                    info.Add(fileName, new StreamInfo() { Length = file.Length, Hash = hash1 });
                }
            }
        }

        static void AddFile(IBinaryStorage storage, string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                storage.Add(fileName, file, StreamInfo.Empty);
            }
        }

        static void CheckFile(IBinaryStorage storage, string fileName, StreamInfo info)
        {
            using (var resultStream = storage.Get(fileName))
            {
                CheckLength(info.Length.Value, resultStream.Length, fileName);

                using (var md5 = MD5.Create())
                {
                    var hash2 = md5.ComputeHash(resultStream);

                    CheckHash(info.Hash, hash2, fileName);
                }
            }
        }

        static void RecheckFile(IBinaryStorage storage, string fileName)
        {
            using (var sourceStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var resultStream = storage.Get(fileName))
                {
                    CheckLength(sourceStream.Length, resultStream.Length, fileName);

                    using (var md5 = MD5.Create())
                    {
                        var hash1 = md5.ComputeHash(sourceStream);

                        md5.Initialize();

                        var hash2 = md5.ComputeHash(resultStream);

                        CheckHash(hash1, hash2, fileName);
                    }
                }
            }
        }

        static void CheckLength(long expectedLength, long actualLength, string fileName)
        {
            if (expectedLength != actualLength)
            {
                throw new Exception($"Length did not match for file - '{fileName}': Source - '{expectedLength}', Result - {actualLength}");
            }
        }

        static void CheckHash(byte[] expectedHash, byte[] actualHash, string fileName)
        {
            if (!expectedHash.SequenceEqual(actualHash))
            {
                throw new Exception($"Hashes do not match for file - '{fileName}'");
            }
        }

        static void AddBytes(IBinaryStorage storage, string key, byte[] data)
        {
            StreamInfo streamInfo = new StreamInfo();
            using (MD5 md5 = MD5.Create())
            {
                streamInfo.Hash = md5.ComputeHash(data);
            }
            streamInfo.Length = data.Length;
            streamInfo.IsCompressed = false;

            using (var ms = new MemoryStream(data))
            {
                storage.Add(key, ms, streamInfo);
            }
        }

        static void Dump(IBinaryStorage storage, string key, string fileName)
        {
            using (var file = new FileStream(fileName, FileMode.Create))
            {
                storage.Get(key).CopyTo(file);
            }
        }
    }
}