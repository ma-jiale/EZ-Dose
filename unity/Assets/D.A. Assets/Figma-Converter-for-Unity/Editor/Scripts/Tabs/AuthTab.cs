using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class AuthTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        private VisualElement root;

        public override void OnLink()
        {
            base.OnLink();
            monoBeh.Authorizer.OnAuthUpdated += Rebuild;
            _ = monoBeh.Authorizer.Init();
        }

        public VisualElement Draw()
        {
            root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);
            BuildUI(root);
            return root;
        }

        private void Rebuild()
        {
            if (root == null)
            {
                return;
            }

            root.Clear();
            BuildUI(root);
        }

        private void BuildUI(VisualElement parent)
        {
            parent.Add(uitk.CreateTitle(FcuLocKey.label_figma_auth.Localize()));
            parent.Add(uitk.Space10());

            DrawAuthenticationPanel(parent);
            parent.Add(uitk.Space10());
            DrawOAuthConfigPanel(parent);
        }

        private void DrawAuthenticationPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            if (monoBeh.Authorizer.Options.IsEmpty() == false)
            {
                List<string> options = monoBeh.Authorizer.Options.Select(o => o.text).ToList();
                var sessionDropdown = uitk.DropdownField(FcuLocKey.label_recent_sessions.Localize(), null);
                sessionDropdown.choices = options;
                sessionDropdown.index = monoBeh.Authorizer.SelectedTableIndex;

                sessionDropdown.RegisterValueChangedCallback(evt =>
                {
                    int selectedIndex = options.IndexOf(evt.newValue);
                    if (selectedIndex != -1)
                    {
                        monoBeh.Authorizer.SelectedTableIndex = selectedIndex;
                        monoBeh.Authorizer.CurrentSession = monoBeh.Authorizer.RecentSessions[selectedIndex];
                    }
                });

                panel.Add(sessionDropdown);
                panel.Add(uitk.ItemSeparator());
            }

            {
                VisualElement tokenInputContainer = new VisualElement();
                tokenInputContainer.style.flexDirection = FlexDirection.Row;
                tokenInputContainer.style.alignItems = Align.Center;
                panel.Add(tokenInputContainer);

                try
                {
                    TextField tokenField = uitk.TextField(FcuLocKey.label_access_token.Localize());
                    tokenField.value = monoBeh.Authorizer.Token;

                    try
                    {
                        tokenField.isPasswordField = true;
                    }
                    catch (Exception ex0)
                    {
                        Debug.LogException(ex0);
                    }

                    tokenField.style.flexGrow = 1;
                    tokenField.RegisterValueChangedCallback(evt =>
                    {
                        FigmaSessionItem session = monoBeh.Authorizer.CurrentSession;
                        AuthResult authResult = session.AuthResult;
                        authResult.AccessToken = evt.newValue;
                        session.AuthResult = authResult;
                        monoBeh.Authorizer.CurrentSession = session;
                    });
                    tokenInputContainer.Add(tokenField);
                }
                catch (System.Exception ex1)
                {
                    Debug.LogException(ex1);
                    Debug.LogError($"{uitk} | {monoBeh} | {monoBeh?.Authorizer} | {monoBeh?.Authorizer?.Token}");
                    Debug.LogError($"Please send these errors to the developer.");
                }

                VisualElement signInTokenButton = uitk.Button(FcuLocKey.auth_button_sign_in_token.Localize(), () =>
                {
                    monoBeh.Authorizer.ManualAuthCts?.Cancel();
                    monoBeh.Authorizer.ManualAuthCts?.Dispose();
                    monoBeh.Authorizer.ManualAuthCts = new CancellationTokenSource();
                    _ = monoBeh.Authorizer.AddNew(new AuthResult
                    {
                        AccessToken = monoBeh.Authorizer.Token
                    }, AuthType.Manual, monoBeh.Authorizer.ManualAuthCts.Token);
                });

                signInTokenButton.style.flexGrow = 0;
                signInTokenButton.style.marginLeft = DAI_UitkConstants.SpacingXXS;
                signInTokenButton.style.maxHeight = 18;
                signInTokenButton.style.marginTop = 1;
                UIHelpers.SetRadius(signInTokenButton, 3);

                tokenInputContainer.Add(signInTokenButton);
            }

            panel.Add(uitk.ItemSeparator());
            panel.Add(uitk.Space5());

            VisualElement buttonsContainer = new VisualElement();
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.justifyContent = Justify.SpaceBetween;
     
            panel.Add(buttonsContainer);

            VisualElement signInWebButton = uitk.Button(FcuLocKey.auth_button_sign_in_web.Localize(), monoBeh.Authorizer.Auth);
            buttonsContainer.Add(signInWebButton);

            buttonsContainer.Add(uitk.Space10());

            VisualElement deleteSessionsButton = uitk.Button(FcuLocKey.auth_button_delete_sessions.Localize(), async () =>
            {
                EditorPrefs.DeleteKey(FcuConfig.FIGMA_SESSIONS_PREFS_KEY);
                await monoBeh.Authorizer.Init();
                Rebuild();
            });
            buttonsContainer.Add(deleteSessionsButton);
        }

        private void DrawOAuthConfigPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label oauthLabel = new Label(FcuLocKey.label_oauth_app_credentials.Localize());
            oauthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(oauthLabel);
            panel.Add(uitk.ItemSeparator());

            EnumFlagsField scopesField = uitk.EnumFlagsField(FcuLocKey.label_oauth_scopes.Localize(), FcuConfig.Scopes);
            scopesField.RegisterValueChangedCallback(evt =>
            {
                FcuConfig.Scopes = (FigmaScope)evt.newValue;
            });
            panel.Add(scopesField);
            panel.Add(uitk.ItemSeparator());

            TextField redirectUriField = uitk.TextField(FcuLocKey.label_redirect_uri.Localize());
            redirectUriField.value = FcuConfig.RedirectUri;
            redirectUriField.RegisterValueChangedCallback(evt => FcuConfig.RedirectUri = evt.newValue);
            panel.Add(redirectUriField);
            panel.Add(uitk.ItemSeparator());

            TextField clientIdField = uitk.TextField(FcuLocKey.label_client_id.Localize());
            clientIdField.value = FcuConfig.ClientId;
            clientIdField.RegisterValueChangedCallback(evt => FcuConfig.ClientId = evt.newValue);
            panel.Add(clientIdField);
            panel.Add(uitk.ItemSeparator());

            TextField clientSecretField = uitk.TextField(FcuLocKey.label_client_secret.Localize());
            clientSecretField.value = FcuConfig.ClientSecret;
            clientSecretField.isPasswordField = true;
            clientSecretField.RegisterValueChangedCallback(evt => FcuConfig.ClientSecret = evt.newValue);
            panel.Add(clientSecretField);
        }
    }
}