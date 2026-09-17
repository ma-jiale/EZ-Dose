using DA_Assets.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.OpenAI
{
    public class GptMini
    {
        private string _apiKey = null;
        private int _timeout = 0;
        private string _model = null;

        public const string _apiUrl = "https://api.openai.com/v1/chat/completions";

        public GptMini(string apiKey, string model = "gpt-4o-mini", int timeout = 0)
        {
            _apiKey = apiKey;
            _model = model;
            _timeout = timeout;
        }

        public async Task<string> InvokeChat(string prompt)
        {
            string methodPath = $"{nameof(GptMini)}.{nameof(InvokeChat)}";

            Debug.Log(SharedLocKey.log_openai_request_started.Localize(methodPath, _model, prompt));

            string chatRequest = CreateChatRequestBody(prompt);

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + _apiKey }
            };

            string jsonResponse = await RequestSender.Post(_apiUrl, chatRequest, "application/json", headers);
            Debug.Log(jsonResponse);
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                Debug.LogError(SharedLocKey.log_openai_empty_response.Localize(methodPath));
                return null;
            }

            Response data = JsonUtility.FromJson<Response>(jsonResponse);

            if (data.choices != null && data.choices.Length > 0)
            {
                return data.choices[0].message.content;
            }
            else
            {
                Debug.LogError(SharedLocKey.log_openai_invalid_response.Localize(methodPath));
                return null;
            }
        }

        private string CreateChatRequestBody(string prompt)
        {
            RequestMessage msg = new RequestMessage();
            msg.role = "user";
            msg.content = prompt;

            Request req = new Request();
            req.model = _model;
            req.messages = new[] { msg };

            return JsonUtility.ToJson(req);
        }
    }

    [Serializable]
    public struct ResponseMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public struct ResponseChoice
    {
        public int index;
        public ResponseMessage message;
    }

    [Serializable]
    public struct Response
    {
        public string id;
        public ResponseChoice[] choices;
    }

    [Serializable]
    public struct RequestMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public struct Request
    {
        public string model;
        public RequestMessage[] messages;
    }
}
