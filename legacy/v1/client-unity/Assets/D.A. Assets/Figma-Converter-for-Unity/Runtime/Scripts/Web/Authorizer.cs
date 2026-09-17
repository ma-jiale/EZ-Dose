using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Linq;
using DA_Assets.DAI;


#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class Authorizer : FcuBase
    {
        [SerializeField] int _selectedTableIndex;
        public int SelectedTableIndex { get => _selectedTableIndex; set => _selectedTableIndex = value; }

        [SerializeField] GUIContent[] _options;
        public GUIContent[] Options { get => _options; set => _options = value; }

        [SerializeField] List<FigmaSessionItem> _recentSessions;
        public List<FigmaSessionItem> RecentSessions { get => _recentSessions; set => _recentSessions = value; }

        public FigmaSessionItem CurrentSession { get; set; }
        public string Token => this.CurrentSession.AuthResult.AccessToken;
        private CancellationTokenSource _authCts;
        public CancellationTokenSource ManualAuthCts;

        public event Action OnAuthUpdated;

        public async Task Init()
        {
            if (!monoBeh.IsJsonNetExists())
            {
                _options = new GUIContent[]
                {
                     new GUIContent($"Newtonsoft.Json is not installed.")
                };

                Debug.LogError(FcuLocKey.log_cant_find_package.Localize(DAConstants.JsonNetPackageName));
                return;
            }

            _recentSessions = await monoBeh.Authorizer.GetSessionItems();

            _options = await GetOptions(false);
            _options = await GetOptions(true);
        }

        private async Task<GUIContent[]> GetOptions(bool includeImages)
        {
            List<GUIContent> options = new List<GUIContent>();

            if (_recentSessions.IsEmpty())
            {
                options.Add(new GUIContent(
                    FcuLocKey.label_no_recent_sessions.Localize(),
                    FcuLocKey.tooltip_no_recent_sessions.Localize()));
            }
            else
            {
                foreach (FigmaSessionItem session in _recentSessions)
                {
                    if (includeImages)
                    {
                        var tex = await DA_Assets.Networking.RequestSender.LoadImage(session.User.ImgUrl);

                        options.Add(new GUIContent($"{session.User.Name} | {session.User.Email}", tex));
                    }
                    else
                    {
                        options.Add(new GUIContent($"{session.User.Name} | {session.User.Email}"));
                    }
                }
            }

            return options.ToArray();
        }

        public void Auth()
        {
            if (monoBeh.IsJsonNetExists() == false)
            {
                Debug.LogError(FcuLocKey.log_cant_find_package.Localize(DAConstants.JsonNetPackageName));
                return;
            }

            monoBeh.AssetTools.StopAsset(StopAssetReason.Hidden);

            _authCts?.Cancel();
            _authCts?.Dispose();
            _authCts = new CancellationTokenSource();
            _ = AuthAsync(_authCts.Token);
        }

        public async Task AuthAsync(CancellationToken token)
        {
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.Authorizing, 0, true);

            try
            {
                DAResult<AuthResult> authResult = await StartAuthThread(token);

                if (authResult.Success)
                {
                    await monoBeh.Authorizer.AddNew(authResult.Object, AuthType.OAuth2, token);
                }
                else
                {
                    Debug.LogError(FcuLocKey.log_cant_auth.Localize(authResult.Error.status, authResult.Error.err, "null"));
                }
            }
            finally
            {
                monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.Authorizing);
            }
        }

        private async Task<DAResult<AuthResult>> StartAuthThread(CancellationToken token)
        {
            string code = "";
            bool gettingCode = true;
            Thread thread = null;

            Debug.Log(FcuLocKey.log_open_auth_page.Localize());

            thread = new Thread(x =>
            {
                Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 1923);

                try
                {
                    server.Bind(endpoint);
                }
                catch (SocketException ex)
                {
                    monoBeh.AssetTools.StopAsset(StopAssetReason.CantAuth, ex);
                    return;
                }
                catch (Exception ex)
                {
                    monoBeh.AssetTools.StopAsset(StopAssetReason.CantAuth, ex);
                    return;
                }

                server.Listen(1);

                Socket socket = server.Accept();

                byte[] bytes = new byte[1000];
                socket.Receive(bytes);
                string rawCode = Encoding.UTF8.GetString(bytes);

                string toSend = "HTTP/1.1 200 OK\nContent-Type: text/html\nConnection: close\n\n" + @"
                    <html>
                        <head>
                            <style type='text/css'>body,html{background-color: #000000;color: #fff;font-family: Segoe UI;text-align: center;}h2{left: 0; position: absolute; top: calc(50% - 25px); width: 100%;}</style>
                            <title>Wait for redirect...</title>
                            <script type='text/javascript'> window.onload=function(){window.location.href='https://figma.com';}</script>
                        </head>
                        <body>
                            <h2>Authorization completed. The page will close automatically.</h2>
                        </body>
                    </html>";

                bytes = Encoding.UTF8.GetBytes(toSend);

                NetworkStream stream = new NetworkStream(socket);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                stream.Close();
                socket.Close();
                server.Close();

                code = rawCode.GetBetween("?code=", "&state=");
                gettingCode = false;
                thread.Abort();
            });

            thread.Start();

            int state = Random.Range(0, int.MaxValue);
            string formattedOauthUrl = string.Format(FcuConfig.OAuthUrl, FcuConfig.ClientId, FcuConfig.RedirectUri, state.ToString());

            Application.OpenURL(formattedOauthUrl);

            while (gettingCode)
            {
                await Task.Delay(100, token);
            }

            DARequest tokenRequest = RequestCreator.CreateTokenRequest(
                code,
                FcuConfig.RedirectUri,
                FcuConfig.ClientId,
                FcuConfig.ClientSecret);

            return await monoBeh.RequestSender.SendRequest<AuthResult>(
                tokenRequest, 
                token);
        }

        public bool IsAuthed()
        {
            if (this.CurrentSession.User.Name.IsEmpty() || this.Token.IsEmpty())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task AddNew(AuthResult authResult, AuthType authType, CancellationToken token)
        {
            DAResult<FigmaUser> result = await GetCurrentFigmaUser(authResult.AccessToken, authType, token);

            if (result.Success)
            {
                FigmaSessionItem newSess = new FigmaSessionItem
                {
                    User = result.Object,
                    AuthResult = authResult,
                    AuthType = authType
                };

                await SetLastSession(newSess);
                await monoBeh.Authorizer.Init();

                OnAuthUpdated?.Invoke();
                DALogger.LogSuccess(FcuLocKey.log_auth_complete.Localize());
            }
            else
            {
                Debug.LogError(FcuLocKey.log_cant_auth.Localize(result.Error.status, result.Error.err, result.Error.exception));
            }
        }

        private async Task<DAResult<FigmaUser>> GetCurrentFigmaUser(string accessToken, AuthType authType, CancellationToken token)
        {
            DARequest request = new DARequest
            {
                Query = "https://api.figma.com/v1/me",
                RequestType = RequestType.Get,
                RequestHeader = monoBeh.RequestSender.GetRequestHeader(accessToken, authType)
            };

            return await monoBeh.RequestSender.SendRequest<FigmaUser>(request, token, FcuLocKey.log_cant_auth.Localize());
        }

        public async Task TryRestoreSession()
        {
            if (monoBeh.IsJsonNetExists() == false)
                return;

            if (IsAuthed() == false)
            {
                FigmaSessionItem item = await GetLastSessionItem();
                this.CurrentSession = item;
            }
        }

        private async Task SetLastSession(FigmaSessionItem sessionItem)
        {
            this.CurrentSession = sessionItem;

            List<FigmaSessionItem> sessionItems = await GetSessionItems();

            FigmaSessionItem targetItem = sessionItems.FirstOrDefault(item => item.AuthResult.AccessToken == sessionItem.AuthResult.AccessToken);
            sessionItems.Remove(targetItem);
            sessionItems.Insert(0, sessionItem);

            if (sessionItems.Count > FcuConfig.FigmaSessionsLimit)
            {
                sessionItems = sessionItems.Take(FcuConfig.FigmaSessionsLimit).ToList();
            }

            SaveDataToPrefs(sessionItems);
        }



        private async Task<FigmaSessionItem> GetLastSessionItem()
        {
            List<FigmaSessionItem> items = await GetSessionItems();
            return items.FirstOrDefault();
        }

        public async Task<List<FigmaSessionItem>> GetSessionItems()
        {
            string json = "";
#if UNITY_EDITOR
            json = UnityEditor.EditorPrefs.GetString(FcuConfig.FIGMA_SESSIONS_PREFS_KEY, "");
#endif
            List<FigmaSessionItem> sessionItems = new List<FigmaSessionItem>();

            if (json.IsEmpty())
                return sessionItems;

            await Task.Run(() =>
            {
                try
                {
                    sessionItems = DAJson.FromJson<List<FigmaSessionItem>>(json);
                }
                catch
                {

                }
            });

            return sessionItems;
        }

        private void SaveDataToPrefs(List<FigmaSessionItem> sessionItems)
        {
#if UNITY_EDITOR && JSONNET_EXISTS
            string json = JsonConvert.SerializeObject(sessionItems);
            UnityEditor.EditorPrefs.SetString(FcuConfig.FIGMA_SESSIONS_PREFS_KEY, json);
#endif
        }
    }
}
