using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using ISQExplorer.Web;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class TestFake
    {
        [Test]
        public async Task TestFakeHtmlClientSerializeDeserialize()
        {
            static string SamplePage(string data) =>
                $"<html><head><link rel='stylesheet' type='text/css' href='style.css'></head><body><h2>{data}</h2></body></html>"
            ;

            static async Task<string> SamplePageMin(string data) =>
                (await HtmlPage.FromHtmlAsync(SamplePage(data))).ToString();

            var fake = new Fake.FakeHtmlClient();

            Assert.AreEqual(fake.Count, 0);

            Assert.AreEqual("[]", fake.Serialize());
            var fake2 = await Fake.FakeHtmlClient.Deserialize("[]");

            Assert.AreEqual(fake2.Count, 0);

            fake.OnGet("https://contoso.com", SamplePage("get_test"));
            fake.OnGet("https://example.com/sample", SamplePage("get_test2"));
            fake.OnGet("https://example.com/sampleNull", null);
            fake.OnPost("https://contoso.com/api", "abc=4;def=7", SamplePage("get_post1"));
            fake.OnPost("https://contoso.com/api", "abc=4;def=8", SamplePage("get_post1"));
            fake.OnPost("https://contoso.com/api", new Dictionary<string, string?>
            {
                {"abc", null},
                {"def", "7"}
            }, SamplePage("get_post1"));
            fake.OnPost("https://contoso.com/api", new Dictionary<string, string?>
            {
                {"abc", null},
                {"def", null}
            }, SamplePage("get_post2"));
            fake.OnPost("https://example.com/blockchain", new Dictionary<string, string?>
            {
                {"abc", null},
                {"def", null}
            }, null);

            var tmp = fake.Serialize();
            Assert.AreEqual(fake.Count, 8);
            var tmp2 = await Fake.FakeHtmlClient.Deserialize(tmp);
            Assert.AreEqual(fake.Count, tmp2.Count);

            Assert.DoesNotThrowAsync(async () =>
            {
                Assert.AreEqual((await tmp2.GetAsync("https://contoso.com")).Value.ToString(),
                    await SamplePageMin("get_test"));
                Assert.AreEqual((await tmp2.GetAsync("https://example.com/sample")).Value.ToString(),
                    await SamplePageMin("get_test2"));
                Assert.False((await tmp2.GetAsync("https://example.com/sampleNull")).HasValue);
                Assert.AreEqual((await tmp2.GetAsync("https://example.com/sample")).Value.ToString(),
                    await SamplePageMin("get_test2"));
                Assert.AreEqual((await tmp2.PostAsync("https://contoso.com/api", "abc=4;def=7")).Value.ToString(),
                    await SamplePageMin("get_post1"));
                Assert.AreEqual((await tmp2.PostAsync("https://contoso.com/api", "abc=4;def=8")).Value.ToString(),
                    await SamplePageMin("get_post1"));
                Assert.AreEqual((await tmp2.PostAsync("https://contoso.com/api", new Dictionary<string, string?>
                {
                    {"abc", null},
                    {"def", "7"}
                })).Value.ToString(), await SamplePageMin("get_post1"));
                Assert.AreEqual((await tmp2.PostAsync("https://contoso.com/api", new Dictionary<string, string?>
                {
                    {"abc", null},
                    {"def", null}
                })).Value.ToString(), await SamplePageMin("get_post2"));
                Assert.False((await tmp2.PostAsync("https://example.com/blockchain", new Dictionary<string, string?>
                {
                    {"abc", null},
                    {"def", null}
                })).HasValue);
            });

            Assert.Pass();
        }
    }
}