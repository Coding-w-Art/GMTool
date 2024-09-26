// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.WinUI.UI.Controls;
using GMTool.Data;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using OpenAI.Models;
using Microsoft.UI.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatBot : Page
    {
        private readonly ObservableCollection<ChatItemData> _chatList;
        private readonly List<Message> _promptHistory;
        private OpenAIRequestType _requestType;
        private OpenAIClient _api;
        private ImageSize _imageSize = ImageSize.Small;
        private string RequestSize => _imageSize switch
        {
            ImageSize.Small => "256x256",
            ImageSize.Medium => "512x512",
            ImageSize.Large => "1024x1024",
            _ => "256x256"
        };

        public ChatBot()
        {
            this.InitializeComponent();

            _promptHistory = new List<Message>();
            _chatList = new ObservableCollection<ChatItemData>();
            chatListView.ItemsSource = _chatList;
            StartNewClient();

            cbRequestType.SelectedIndex = 0;
            cbImageSize.SelectedIndex = 0;
        }

        private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
        {
            _promptHistory.Clear();
            _chatList.Clear();
            StartNewClient();
        }

        private void StartNewClient()
        {
            try
            {
                OpenAIAuthentication authentication = OpenAIAuthentication.LoadFromDirectory("Assets", "openai_apikey");
                _api = new OpenAIClient(authentication);
            }
            catch (Exception)
            {
                btnSend.IsEnabled = false;
            }

            if (_api == null)
            {
                btnSend.IsEnabled = false;
            }
            else
            {
                btnSend.IsEnabled = true;
            }
        }

        private void ButtonSend_OnClick(object sender, RoutedEventArgs e)
        {
            StartCompletion(tbPrompt.Text);
        }

        private async Task<bool> ProcessRequest(string prompt)
        {
            switch (_requestType)
            {
                case OpenAIRequestType.Chat:
                    ChatRequest chatRequest = new ChatRequest(_promptHistory, Model.GPT3_5_Turbo_16K);
                    string output = string.Empty;
                    ChatItemData itemData = new ChatItemData(Role.System, output);

                    bool isFirst = true;
                    await foreach (ChatResponse chatResult in _api.ChatEndpoint.StreamCompletionEnumerableAsync(chatRequest))
                    {
                        if (chatResult.FirstChoice.FinishReason != "stop")
                        {
                            output += chatResult.FirstChoice;
                            itemData.Text = output;
                            if (isFirst)
                            {
                                isFirst = false;
                                _chatList.Add(itemData);
                            }
                        }
                    }
                    _promptHistory.Add(new Message(Role.System, output));
                    return true;
                case OpenAIRequestType.Image:
                    ImageGenerationRequest request =
                        new ImageGenerationRequest(prompt, Model.DallE_2, size: RequestSize);
                    IReadOnlyList<ImageResult> imageResult = await _api.ImagesEndPoint.GenerateImageAsync(request);
                    foreach (string image in imageResult)
                    {
                        _chatList.Add(new ChatItemData(Role.System, $"[![Image]({image})]({image})"));
                    }
                    return imageResult.Count > 0;
                //case OpenAIRequestType.Completion:
                //    CompletionResponse completionResult = await _api.CompletionsEndpoint.CreateCompletionAsync(prompt, model:Model.GPT3_5_Turbo_16K);
                //    foreach (OpenAI.Completions.Choice choice in completionResult.Completions)
                //    {
                //        string text = choice.Text;
                //        _chatList.Add(new ChatItemData(Role.System, text));
                //    }
                //    return completionResult.Completions.Count > 0;
                //case OpenAIRequestType.Speech:
                //    SpeechRequest speechRequest = new SpeechRequest(prompt, Model.Whisper1);
                //    ReadOnlyMemory<byte> result = await _api.AudioEndpoint.CreateSpeechAsync(speechRequest); ;
                //    _chatList.Add(new ChatItemData(Role.System, "..."));
                //    return result.Length > 0;
            }
            return false;
        }

        private async void StartCompletion(string input)
        {
            string prompt = input.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            _promptHistory.Add(new Message(Role.User, prompt));
            _chatList.Add(new ChatItemData(Role.User, prompt));
            tbPrompt.Text = string.Empty;
            chatListView.ScrollIntoView(_chatList[^1]);
            btnSend.IsHitTestVisible = false;
            prSend.IsActive = true;
            fiSend.Visibility = Visibility.Collapsed;

            try
            {
                bool result = await ProcessRequest(prompt);
                if (!result)
                {
                    _chatList.Add(new ChatItemData(Role.Assistant, "没有获得结果，请重试"));
                }
            }
            catch (Exception ex)
            {
                _chatList.Add(new ChatItemData(Role.Assistant, ex.Message));
            }

            prSend.IsActive = false;
            fiSend.Visibility = Visibility.Visible;
            btnSend.IsHitTestVisible = true;
        }

        private void OnRequestTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _requestType = (OpenAIRequestType)comboBox.SelectedIndex;
                cbImageSize.Visibility = _requestType == OpenAIRequestType.Image ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnImageSizeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                _imageSize = (ImageSize)comboBox.SelectedIndex;
            }
        }

        private void TbPrompt_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            inputRow.Height = new GridLength(tbPrompt.ActualHeight, GridUnitType.Auto);
        }

        private async void MarkdownLinkClicked(object sender, LinkClickedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(e.Link));
        }
        private static bool IsCtrlKeyPressed()
        {
            var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            return (ctrlState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        private void OnPromptKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (btnSend.IsHitTestVisible && IsCtrlKeyPressed() && e.Key == VirtualKey.Enter)
            {
                StartCompletion(tbPrompt.Text);
            }
        }
    }
}
