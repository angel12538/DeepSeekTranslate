using HttpWebRequestWrapper;
using System.Net;
using Xunit;

namespace DeepSeekTranslate.Tests
{
    public class DisableThinkingTests : BaseErrorHandlingTests
    {
        private readonly string _fakeGoodResponseBody = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": ""{\""0\"": \""你好\""}""
                    }
                }
            ]
        }";

        [Fact]
        public void TranslateSingle_DisableThinking_True_RequestContainsDisabledThinking()
        {
            string capturedRequestBody = null;

            using (new HttpWebRequestWrapperSession(
                new HttpWebRequestWrapperInterceptorCreator(request =>
                {
                    capturedRequestBody = request.RequestPayload.SerializedStream;
                    return request.HttpWebResponseCreator.Create(_fakeGoodResponseBody, HttpStatusCode.OK);
                })))
            {
                var endpoint = CreateEndpoint(useThreadPool: false, disableThinking: true);
                var context = new TestTranslationContext("Hello");

                var enumerator = endpoint.Translate(context);
                while (enumerator.MoveNext()) { }
            }

            Assert.NotNull(capturedRequestBody);
            Assert.Contains("\"thinking\":{\"type\":\"disabled\"}", capturedRequestBody);
        }

        [Fact]
        public void TranslateSingle_DisableThinking_False_RequestDoesNotContainThinkingField()
        {
            string capturedRequestBody = null;

            using (new HttpWebRequestWrapperSession(
                new HttpWebRequestWrapperInterceptorCreator(request =>
                {
                    capturedRequestBody = request.RequestPayload.SerializedStream;
                    return request.HttpWebResponseCreator.Create(_fakeGoodResponseBody, HttpStatusCode.OK);
                })))
            {
                var endpoint = CreateEndpoint(useThreadPool: false, disableThinking: false);
                var context = new TestTranslationContext("Hello");

                var enumerator = endpoint.Translate(context);
                while (enumerator.MoveNext()) { }
            }

            Assert.NotNull(capturedRequestBody);
            Assert.DoesNotContain("\"thinking\"", capturedRequestBody);
        }
    }
}
