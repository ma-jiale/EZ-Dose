using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DA_Assets.FCU
{
    public class RequestCreator
    {
        public static DARequest CreateImageLinksRequest(string projectUrl, string format, float scale, IEnumerable<string> chunk, RequestHeader requestHeader)
        {
            string query = CreateImagesQuery(
                    chunk,
                    projectUrl,
                    format,
                    scale);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static string CreateImagesQuery(
            IEnumerable<string> chunk, 
            string projectId, 
            string extension,
            float scale)
        {
            string joinedIds = string.Join(",", chunk);

            if (string.IsNullOrWhiteSpace(joinedIds))
                return null;

            if (joinedIds[0] == ',')
                joinedIds = joinedIds.Remove(0, 1);

            string query = $"https://api.figma.com/v1/images/{projectId}?ids={joinedIds}&format={extension}&scale={scale.ToString(CultureInfo.InvariantCulture)}";
            return query;
        }

        public static DARequest CreateTokenRequest(
            string code,
            string redirectUri,
            string clientId,
            string clientSecret)
        {
            string tokenUrl = "https://api.figma.com/v1/oauth/token";

            DARequest request = new DARequest
            {
                Query = tokenUrl,
                RequestType = RequestType.Post,
                WWWForm = new WWWForm()
            };

            request.WWWForm.AddField("grant_type", "authorization_code");
            request.WWWForm.AddField("code", code);
            request.WWWForm.AddField("redirect_uri", redirectUri);

            string credentials = $"{clientId}:{clientSecret}";
            string encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));

            request.RequestHeader = new RequestHeader
            {
                Name = "Authorization",
                Value = $"Basic {encodedCredentials}"
            };

            return request;
        }

        public static DARequest CreateProjectRequest(RequestHeader requestHeader, string projectId, int frameListDepth)
        {
            string query = string.Format("https://api.figma.com/v1/files/{0}?depth={1}&plugin_data=shared", projectId, frameListDepth);

            DARequest request = new DARequest
            {
                Name = RequestName.Project,
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static DARequest CreateNodeRequest(RequestHeader requestHeader, string projectId, string nodeIds)
        {
            string query = string.Format("https://api.figma.com/v1/files/{0}/nodes?ids={1}&geometry=paths&plugin_data=shared", projectId, nodeIds);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }

        public static DARequest CreateFileStructRequest(RequestHeader requestHeader, string projectId, int depth)
        {
            string query = string.Format("https://api.figma.com/v1/files/{0}?depth={1}", projectId, depth);

            DARequest request = new DARequest
            {
                Query = query,
                RequestType = RequestType.Get,
                RequestHeader = requestHeader
            };

            return request;
        }
    }
}
