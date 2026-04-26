using HttpWebRequestWrapper;
using System;
using System.Net;
using Xunit;

namespace DeepSeekTranslate.Tests
{
    public class JsonEscapingTests : BaseErrorHandlingTests
    {
        [Fact]
        public void TranslateSingle_RequestJson_EscapesSpecialCharacters()
        {
            var sourceText = "Line1\nSay \"hello\" at path\\to\\file";

            var requestBody = CaptureRequestBody(
                () => CreateEndpoint(useThreadPool: false),
                new TestTranslationContext(sourceText));

            var promptJson = ExtractPromptJson(requestBody);

            Assert.Equal("{\"0\": \"Line1\\nSay \\\"hello\\\" at path\\\\to\\\\file\"}", promptJson);
        }

        [Fact]
        public void TranslateBatch_RequestJson_EscapesSpecialCharacters()
        {
            var sourceText = "Line1\nSay \"hello\" at path\\to\\file";

            var requestBody = CaptureRequestBody(
                () => CreateEndpoint(batchTranslate: true, useThreadPool: true),
                new TestTranslationContext(new[] { sourceText, "World" }));

            var promptJson = ExtractPromptJson(requestBody);

            Assert.Equal("{\"0\":\"Line1\\nSay \\\"hello\\\" at path\\\\to\\\\file\",\"1\":\"World\"}", promptJson);
        }

        [Fact]
        public void TranslateSingle_ResponseJson_IsUnescapedBeforeCompleting()
        {
            var translatedText = "Line1\n\"hello\" at path\\to\\file";
            var responseBody = BuildSuccessResponse("{\"0\":\"" + EscapeJsonString(translatedText) + "\"}");

            using (new HttpWebRequestWrapperSession(
                new HttpWebRequestWrapperInterceptorCreator(request =>
                    request.HttpWebResponseCreator.Create(responseBody, HttpStatusCode.OK))))
            {
                var endpoint = CreateEndpoint(useThreadPool: false);
                var context = new TestTranslationContext("source");

                var enumerator = endpoint.Translate(context);
                while (enumerator.MoveNext()) { }

                Assert.True(context.IsDone);
                Assert.Equal(translatedText, context.TranslatedText);
            }
        }

        [Fact]
        public void TranslateBatch_ResponseJson_IsUnescapedBeforeCompleting()
        {
            var firstTranslatedText = "Line1\n\"hello\" at path\\to\\file";
            var secondTranslatedText = "Second\\value";
            var responseBody = BuildSuccessResponse(
                "{\"0\":\"" + EscapeJsonString(firstTranslatedText) + "\",\"1\":\"" + EscapeJsonString(secondTranslatedText) + "\"}");

            using (new HttpWebRequestWrapperSession(
                new HttpWebRequestWrapperInterceptorCreator(request =>
                    request.HttpWebResponseCreator.Create(responseBody, HttpStatusCode.OK))))
            {
                var endpoint = CreateEndpoint(batchTranslate: true, useThreadPool: true);
                var context = new TestTranslationContext(new[] { "first", "second" });

                var enumerator = endpoint.Translate(context);
                while (enumerator.MoveNext()) { }

                Assert.True(context.IsDone);
                Assert.NotNull(context.TranslatedTexts);
                Assert.Equal(firstTranslatedText, context.TranslatedTexts[0]);
                Assert.Equal(secondTranslatedText, context.TranslatedTexts[1]);
            }
        }

        private static string CaptureRequestBody(Func<DeepSeekTranslateEndpoint> createEndpoint, TestTranslationContext context)
        {
            string capturedRequestBody = null;
            var responseBody = context.UntranslatedTexts.Length > 1
                ? BuildSuccessResponse("{\"0\":\"ok\",\"1\":\"ok\"}")
                : BuildSuccessResponse("{\"0\":\"ok\"}");

            using (new HttpWebRequestWrapperSession(
                new HttpWebRequestWrapperInterceptorCreator(request =>
                {
                    capturedRequestBody = request.RequestPayload.SerializedStream;
                    return request.HttpWebResponseCreator.Create(responseBody, HttpStatusCode.OK);
                })))
            {
                var endpoint = createEndpoint();
                var enumerator = endpoint.Translate(context);
                while (enumerator.MoveNext()) { }
            }

            Assert.NotNull(capturedRequestBody);
            return capturedRequestBody;
        }

        private static string ExtractPromptJson(string requestBody)
        {
            const string startMarker = "```json\\n";
            const string endMarker = "\\n```";

            var start = requestBody.LastIndexOf(startMarker, StringComparison.Ordinal);
            Assert.True(start >= 0, "Could not find the JSON code block in the request body.");

            start += startMarker.Length;

            var end = requestBody.IndexOf(endMarker, start, StringComparison.Ordinal);
            Assert.True(end >= 0, "Could not find the end of the JSON code block in the request body.");

            return DecodeOuterJsonString(requestBody.Substring(start, end - start));
        }

        private static string BuildSuccessResponse(string jsonObject)
        {
            return "{\"choices\":[{\"message\":{\"content\":\"" + EscapeJsonString(jsonObject) + "\"}}]}";
        }

        private static string EscapeJsonString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string DecodeOuterJsonString(string value)
        {
            var result = new System.Text.StringBuilder(value.Length);

            for (int index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (current != '\\' || index == value.Length - 1)
                {
                    result.Append(current);
                    continue;
                }

                index++;
                var escaped = value[index];
                switch (escaped)
                {
                    case '\\':
                        result.Append('\\');
                        break;
                    case '"':
                        result.Append('"');
                        break;
                    case 'n':
                        result.Append('\n');
                        break;
                    case 'r':
                        result.Append('\r');
                        break;
                    case 't':
                        result.Append('\t');
                        break;
                    default:
                        result.Append('\\');
                        result.Append(escaped);
                        break;
                }
            }

            return result.ToString();
        }
    }
}

