using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AngleSharp.Common;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using ISQExplorer.Models;
using ISQExplorer.Repositories;
using ISQExplorer.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace ISQExplorerTests
{
    public static class Fake
    {
        public static Mock<ICourseRepository> CourseRepository(out IList<CourseModel> courseList)
        {
            var mock = new Mock<ICourseRepository>();

            var fakeDept = new DepartmentModel
            {
                Id = 0,
                Name = "Mocked"
            };

            mock.Setup(cr => cr.FromCourseCodeAsync(It.IsAny<string>()))
                .Returns((string s) => Task.FromResult(new Optional<CourseModel>(new CourseModel
                {
                    Department = fakeDept,
                    CourseCode = s,
                    Name = "Mocked"
                })));

            mock.Setup(cr => cr.FromCourseNameAsync(It.IsAny<string>()))
                .Returns((string s) => Task.FromResult(new Optional<CourseModel>(new CourseModel
                {
                    Department = fakeDept,
                    CourseCode = s,
                    Name = "Mocked"
                })));

            var tmp = new List<CourseModel>();

            mock.Setup(cr => cr.AddAsync(It.IsAny<CourseModel>()))
                .Returns((CourseModel cm) => Task.Run(() => tmp.Add(cm)));
            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<CourseModel>>()))
                .Returns((IEnumerable<CourseModel> models) => Task.Run(() => tmp.AddRange(models)));

            courseList = tmp;

            return mock;
        }

        public static Mock<IDepartmentRepository> DepartmentRepository(out IList<DepartmentModel> departments)
        {
            var mock = new Mock<IDepartmentRepository>();
            mock.Setup(dr => dr.FromNameAsync(It.IsAny<string>()))
                .Returns((string s) => Task.FromResult(new Optional<DepartmentModel>(new DepartmentModel
                {
                    Id = 0,
                    Name = s
                })));

            mock.Setup(dr => dr.FromIdAsync(It.IsAny<int>()))
                .Returns((int i) => Task.FromResult(new Optional<DepartmentModel>(new DepartmentModel
                {
                    Id = i,
                    Name = "Mocked"
                })));

            var tmp = new List<DepartmentModel>();

            mock.Setup(cr => cr.AddAsync(It.IsAny<DepartmentModel>()))
                .Returns((DepartmentModel cm) => Task.Run(() => tmp.Add(cm)));

            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<DepartmentModel>>()))
                .Returns((DepartmentModel cm) => Task.Run(() => tmp.Add(cm)));
            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<DepartmentModel>>()))
                .Returns((IEnumerable<DepartmentModel> models) => Task.Run(() => tmp.AddRange(models)));

            departments = tmp;

            return mock;
        }

        public static Mock<IEntryRepository> EntryRepository(out IList<ISQEntryModel> entries)
        {
            var mock = new Mock<IEntryRepository>();

            var tmp = new List<ISQEntryModel>();

            mock.Setup(cr => cr.ByCourseAsync(It.IsAny<CourseModel>(), It.IsAny<TermModel?>(), It.IsAny<TermModel?>()))
                .Returns(Task.FromResult(Array.Empty<ISQEntryModel>().AsEnumerable()));
            mock.Setup(cr =>
                    cr.ByProfessorAsync(It.IsAny<ProfessorModel>(), It.IsAny<TermModel?>(), It.IsAny<TermModel?>()))
                .Returns(Task.FromResult(Array.Empty<ISQEntryModel>().AsEnumerable()));

            mock.Setup(cr => cr.AddAsync(It.IsAny<ISQEntryModel>()))
                .Returns((ISQEntryModel cm) => Task.Run(() => tmp.Add(cm)));
            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<ISQEntryModel>>()))
                .Returns((IEnumerable<ISQEntryModel> models) => Task.Run(() => tmp.AddRange(models)));

            entries = tmp;

            return mock;
        }

        public static Mock<IProfessorRepository> ProfessorRepository(out IList<ProfessorModel> professors)
        {
            var mock = new Mock<IProfessorRepository>();
            mock.Setup(dr => dr.FromFirstNameAsync(It.IsAny<DepartmentModel>(), It.IsAny<string>()))
                .Returns((DepartmentModel dept, string s) => Task.FromResult(new Optional<ProfessorModel>(
                    new ProfessorModel
                    {
                        Id = 0,
                        Department = dept,
                        FirstName = s,
                        LastName = "Mocked",
                        NNumber = "N00000000"
                    })));

            mock.Setup(dr => dr.FromLastNameAsync(It.IsAny<DepartmentModel>(), It.IsAny<string>()))
                .Returns((DepartmentModel dept, string s) => Task.FromResult(new Optional<ProfessorModel>(
                    new ProfessorModel
                    {
                        Id = 0,
                        Department = dept,
                        FirstName = "Mocked",
                        LastName = s,
                        NNumber = "N00000000"
                    })));

            mock.Setup(dr => dr.FromNameAsync(It.IsAny<DepartmentModel>(), It.IsAny<string>()))
                .Returns((DepartmentModel dept, string s) => Task.FromResult(new Optional<ProfessorModel>(
                    new ProfessorModel
                    {
                        Id = 0,
                        Department = dept,
                        FirstName = s.Split(" ").SkipLast(1).Join(" "),
                        LastName = s.Split(" ").Last(),
                        NNumber = "N00000000"
                    })));

            mock.Setup(dr => dr.FromNNumberAsync(It.IsAny<DepartmentModel>(), It.IsAny<string>()))
                .Returns((DepartmentModel dept, string s) => Task.FromResult(new Optional<ProfessorModel>(
                    new ProfessorModel
                    {
                        Id = 0,
                        Department = dept,
                        FirstName = "Mocked",
                        LastName = "Mocked",
                        NNumber = s
                    })));

            var tmp = new List<ProfessorModel>();

            mock.Setup(cr => cr.AddAsync(It.IsAny<ProfessorModel>()))
                .Returns((ProfessorModel cm) => Task.Run(() => tmp.Add(cm)));

            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<ProfessorModel>>()))
                .Returns((ProfessorModel cm) => Task.Run(() => tmp.Add(cm)));
            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<ProfessorModel>>()))
                .Returns((IEnumerable<ProfessorModel> models) => Task.Run(() => tmp.AddRange(models)));

            professors = tmp;

            return mock;
        }

        public static Mock<ITermRepository> TermRepository(out IList<TermModel> terms)
        {
            var mock = new Mock<ITermRepository>();
            mock.Setup(tr => tr.FromStringAsync(It.IsAny<string>()))
                .Returns((string s) => Task.FromResult(new Optional<TermModel>(new TermModel
                {
                    Id = 0,
                    Name = s
                })));

            mock.Setup(tr => tr.FromIdAsync(It.IsAny<int>()))
                .Returns((int i) => Task.FromResult(new Optional<TermModel>(new TermModel
                {
                    Id = i,
                    Name = "Mocked"
                })));

            var tmp = new List<TermModel>();

            mock.Setup(cr => cr.AddAsync(It.IsAny<TermModel>()))
                .Returns((TermModel cm) => Task.Run(() => tmp.Add(cm)));

            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<TermModel>>()))
                .Returns((TermModel cm) => Task.Run(() => tmp.Add(cm)));
            mock.Setup(cr => cr.AddRangeAsync(It.IsAny<IEnumerable<TermModel>>()))
                .Returns((IEnumerable<TermModel> models) => Task.Run(() => tmp.AddRange(models)));

            terms = tmp;

            return mock;
        }

        public class FakeHtmlClient : IHtmlClient
        {
            public class
                PostDataComparer : IEqualityComparer<(Either<Uri, string>, ImmutableDictionary<string, string>?)>
            {
                public bool Equals((Either<Uri, string>, ImmutableDictionary<string, string>?) x,
                    (Either<Uri, string>, ImmutableDictionary<string, string>?) y)
                {
                    return x.Item1.Unite(uri => uri.ToString()) == y.Item1.Unite(uri => uri.ToString()) &&
                           x.Item2.SequenceEqual(y.Item2);
                }

                public int GetHashCode((Either<Uri, string>, ImmutableDictionary<string, string>?) obj)
                {
                    unchecked
                    {
                        var b = obj.Item1.Unite(uri => uri.ToString()).GetHashCode();
                        return obj.Item2.OrderBy(x => x.Key)
                            .ThenBy(x => x.Value)
                            .Aggregate(b, (a, c) => a * 31 + c.GetHashCode());
                    }
                }
            }

            public class PostStringComparer : IEqualityComparer<(Either<Uri, string>, string)>
            {
                public bool Equals((Either<Uri, string>, string) x, (Either<Uri, string>, string) y)
                {
                    return x.Item1.Unite(uri => uri.ToString()) == y.Item1.Unite(uri => uri.ToString()) &&
                           x.Item2 == y.Item2;
                }

                public int GetHashCode((Either<Uri, string>, string) obj)
                {
                    unchecked
                    {
                        return HashCode.Combine(obj.Item1.Unite(uri => uri.ToString()).GetHashCode(), obj.Item2);
                    }
                }
            }

            public class GetDataComparer : IEqualityComparer<Either<Uri, string>>
            {
                public bool Equals(Either<Uri, string> x, Either<Uri, string> y)
                {
                    return x?.Unite(uri => uri.ToString()) == y?.Unite(uri => uri.ToString());
                }

                public int GetHashCode(Either<Uri, string> obj)
                {
                    return obj.Unite(uri => uri.ToString()).GetHashCode();
                }
            }

            private HtmlClient? _realHtmlClient;
            private readonly ConcurrentDictionary<Either<Uri, string>, Try<HtmlPage, IOException>> _getMocks;

            private Action<string>? _afterGetCallback;
            private Action<string, IReadOnlyDictionary<string, string?>>? _afterPostDictCallback;
            private Action<string, string>? _afterPostStringCallback;

            private readonly ConcurrentDictionary<(Either<Uri, string>, string), Try<HtmlPage, IOException>>
                _postStringMocks;

            private readonly
                ConcurrentDictionary<(Either<Uri, string>, ImmutableDictionary<string, string?>),
                    Try<HtmlPage, IOException>> _postDictMocks;

            public FakeHtmlClient()
            {
                _getMocks =
                    new ConcurrentDictionary<Either<Uri, string>, Try<HtmlPage, IOException>>(new GetDataComparer());
                _postStringMocks =
                    new ConcurrentDictionary<(Either<Uri, string>, string), Try<HtmlPage, IOException>>(
                        new PostStringComparer());
                _postDictMocks =
                    new ConcurrentDictionary<(Either<Uri, string>, ImmutableDictionary<string, string?>),
                        Try<HtmlPage, IOException>>(new PostDataComparer()!);
            }

            public FakeHtmlClient OnGet(Either<Uri, string> url, string? returnPage)
            {
                _getMocks[url] = returnPage != null
                    ? new Try<HtmlPage, IOException>(HtmlPage.FromHtmlAsync(returnPage).Result)
                    : new IOException("The page was mocked to throw an IOException.");
                return this;
            }

            public FakeHtmlClient SetAfterGetCallback(Action<string>? callback)
            {
                _afterGetCallback = callback;
                return this;
            }

            public FakeHtmlClient SetAfterPostDictCallback(
                Action<string, IReadOnlyDictionary<string, string?>>? callback)
            {
                _afterPostDictCallback = callback;
                return this;
            }

            public FakeHtmlClient SetAfterPostStringCallback(Action<string, string>? callback)
            {
                _afterPostStringCallback = callback;
                return this;
            }

            public FakeHtmlClient OnPost(Either<Uri, string> url,
                Either<string, IReadOnlyDictionary<string, string?>> postData, string? returnPage)
            {
                var val = returnPage != null
                    ? new Try<HtmlPage, IOException>(HtmlPage.FromHtmlAsync(returnPage).Result)
                    : new IOException("The page was mocked to throw an IOException.");

                postData.Match(
                    left => _postStringMocks[(url, left)] = val,
                    right => _postDictMocks[(url, right.ToImmutableDictionary())] = val
                );

                return this;
            }

            public FakeHtmlClient DefaultToException()
            {
                _realHtmlClient = null;
                return this;
            }

            public FakeHtmlClient DefaultToWeb()
            {
                _realHtmlClient = new HtmlClient(() => new RateLimiter(3, 1000));
                return this;
            }

            public FakeHtmlClient DefaultToWeb(Func<string, RateLimiter>? limiterFactory)
            {
                _realHtmlClient = new HtmlClient(limiterFactory);
                return this;
            }

            public FakeHtmlClient RemoveExceptions()
            {
                _getMocks
                    .Where(x => !x.Value.HasValue)
                    .ForEach(x => _getMocks.TryRemove(x.Key, out _));

                _postDictMocks
                    .Where(x => !x.Value.HasValue)
                    .ForEach(x => _postDictMocks.TryRemove(x.Key, out _));

                _postStringMocks
                    .Where(x => !x.Value.HasValue)
                    .ForEach(x => _postStringMocks.TryRemove(x.Key, out _));

                return this;
            }

            public async Task<Try<HtmlPage, IOException>> GetAsync(Either<Uri, string> url)
            {
                if (_getMocks.ContainsKey(url))
                {
                    return _getMocks[url];
                }

                var res = await (_realHtmlClient != null
                    ? _realHtmlClient.GetAsync(url)
                    : Task.FromResult(new Try<HtmlPage, IOException>(new IOException($"Url '{url}' was not mocked."))));

                _getMocks[url] = res;

                _afterGetCallback?.Invoke(url.Unite(uri => uri.ToString()));

                return res;
            }

            public async Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url, string postData)
            {
                if (_postStringMocks.ContainsKey((url, postData)))
                {
                    return _postStringMocks[(url, postData)];
                }

                var res = await (_realHtmlClient != null
                    ? _realHtmlClient.PostAsync(url, postData)
                    : Task.FromResult(new Try<HtmlPage, IOException>(new IOException($"Url '{url}' was not mocked."))));

                _postStringMocks[(url, postData)] = res;

                _afterPostStringCallback?.Invoke(url.Unite(uri => uri.ToString()), postData);

                return res;
            }

            public async Task<Try<HtmlPage, IOException>> PostAsync(Either<Uri, string> url,
                IReadOnlyDictionary<string, string?> postParams)
            {
                var immut = postParams.ToImmutableDictionary();
                if (_postDictMocks.ContainsKey((url, immut)))
                {
                    return _postDictMocks[(url, immut)];
                }

                var res = await (_realHtmlClient != null
                    ? _realHtmlClient.PostAsync(url, postParams)
                    : Task.FromResult(new Try<HtmlPage, IOException>(new IOException($"Url '{url}' was not mocked."))));

                _postDictMocks[(url, immut)] = res;

                _afterPostDictCallback?.Invoke(url.Unite(uri => uri.ToString()), postParams);

                return res;
            }

            public int Count => _getMocks.Count + _postDictMocks.Count + _postStringMocks.Count;

            public string Serialize()
            {
                var entries = _getMocks.Select(x => new SerializationUrlEntry
                    {
                        Url = x.Key.Unite(uri => uri.ToString()),
                        Data = x.Value.HasValue ? x.Value.Value.ToString() : null,
                    })
                    .Concat(_postDictMocks.Select(x => new SerializationUrlEntry
                    {
                        Url = x.Key.Item1.Unite(uri => uri.ToString()),
                        PostDataDict = x.Key.Item2,
                        Data = x.Value.HasValue ? x.Value.Value.ToString() : null
                    }))
                    .Concat(_postStringMocks.Select(x => new SerializationUrlEntry
                    {
                        Url = x.Key.Item1.Unite(uri => uri.ToString()),
                        PostDataString = x.Key.Item2,
                        Data = x.Value.HasValue ? x.Value.Value.ToString() : null
                    }));

                if (_realHtmlClient != null)
                {
                    entries = entries.Concat(_realHtmlClient.GetCache.Select(x => new SerializationUrlEntry
                        {
                            Url = x.Key.ToString(),
                            Data = x.Value.ToString()
                        }))
                        .Concat(_realHtmlClient.PostCache.Select(x => new SerializationUrlEntry
                        {
                            Url = x.Key.Item1.ToString(),
                            PostDataString = x.Key.Item2,
                            Data = x.Value.ToString()
                        }));
                }

                return JsonConvert.SerializeObject(entries);
            }

            public static async Task<FakeHtmlClient> Deserialize(string input)
            {
                static async Task<Try<HtmlPage, IOException>> ConvData(string? data) => data != null
                    ? await HtmlPage.FromHtmlAsync(data)
                    : new Try<HtmlPage, IOException>(new IOException("The page was mocked to throw an IOException."));

                var ret = new FakeHtmlClient();

                var entries = JsonConvert.DeserializeObject<List<SerializationUrlEntry>>(input);
                await entries.AsParallel().ForEachAsync(async ent =>
                {
                    if (ent.PostDataDict != null)
                    {
                        ret._postDictMocks[(ent.Url, ent.PostDataDict.ToImmutableDictionary())] =
                            await ConvData(ent.Data);
                    }
                    else if (ent.PostDataString != null)
                    {
                        ret._postStringMocks[(ent.Url, ent.PostDataString)] = await ConvData(ent.Data);
                    }
                    else
                    {
                        ret._getMocks[ent.Url] = await ConvData(ent.Data);
                    }
                });

                return ret;
            }

            public class SerializationUrlEntry
            {
                public string Url { get; set; } = null!;
                public IReadOnlyDictionary<string, string?>? PostDataDict;
                public string? PostDataString { get; set; }
                public string? Data { get; set; }
            }
        }

        public static class Compressor
        {
            public static Task<byte[]> Compress(string input) => Task.Run(() =>
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                using var ms = new MemoryStream();

                using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
                {
                    gz.Write(bytes);
                }

                return ms.GetBuffer();
            });

            public static Task<string> Decompress(byte[] input) => Task.Run(() =>
            {
                using var mso = new MemoryStream();

                var buf = new byte[65536];

                using (var msi = new MemoryStream(input))
                {
                    using var gz = new GZipStream(msi, CompressionMode.Decompress);
                    int count;

                    while ((count = gz.Read(buf, 0, buf.Length)) > 0)
                    {
                        mso.Write(buf, 0, count);
                    }
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            });
        }

        public static ISQExplorerContext DbContext()
        {
            File.Delete("test.db");
            
            var options = new DbContextOptionsBuilder<ISQExplorerContext>()
                .UseSqlite("DataSource=test.db")
                .Options;

            var ctx = new ISQExplorerContext(options);
            ctx.Database.EnsureCreated();
            
            return ctx;
        }
    }
}