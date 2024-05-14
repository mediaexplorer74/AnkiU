﻿/*
Copyright (C) 2016 Anki Universal Team <ankiuniversal@outlook.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using AnkiU.UIUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AnkiU.UserControls
{
    public sealed partial class SpeechSynthesis : UserControl
    {
        private bool isNarrowState = true;
        private const double MIN_PLAYBACK_RATE = 0.25;
        private const double MAX_PLAYBACK_RATE = 2;
        private SpeechSynthesizer synthesizer;
        private SpeechSynthesisStream synthesisStream;
        private string textToSynth;

        private bool isSuppressVoiceChangedEvent = false;

        public delegate void PlayBackRateChangedHandler(double rate);

        public event RoutedEventHandler PlayButtonClick;
        public event PlayBackRateChangedHandler PlayBackRateChanged;
        public event RoutedEventHandler VoiceChanged;

        public bool IsPlaying { get; private set; }

        public SpeechSynthesis()
        {                        
            this.InitializeComponent();     
            InitSynthesizer();
            InitializeListboxVoiceChooser();                        
        }

        public void InitSynthesizer()
        {
            synthesizer = new SpeechSynthesizer();
            textToSynth = null;
        }

        public void ChangeSpeechSyntViewColor(bool isNightMode)
        {
            if (isNightMode)
            {
                userControl.Background = UIHelper.DarkerBrush;
                userControl.Foreground = UIHelper.ForeGroundLight;
            }
            else
            {
                userControl.Background = UIHelper.BackgroundWhiteNormal;
                userControl.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            }

        }

        private void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            PlayButtonClick?.Invoke(sender, e);
        }

        public async Task StartTextToSpeech(string text)
        {
            if (String.IsNullOrEmpty(text) || IsPlaying)
                return;

            IsPlaying = true;
            SwitchToPauseSymbol();

            try
            {
                playButton.IsEnabled = false;
                await StartMediaSource(text);
            }
            catch (FileNotFoundException)
            {
                await UnvailableMediaPlayerExceptionHandler();
            }
            catch (Exception)
            {
                await UnableToSynthExceptionHandler();
            }
            finally
            {
                playButton.IsEnabled = true;
            }
        }

        private void SwitchToPauseSymbol()
        {
            playButtonSymbol.Visibility = Visibility.Collapsed;
            pauseButtonSymbol.Visibility = Visibility.Visible;
        }

        private void SwitchToPlaySymbol()
        {
            playButtonSymbol.Visibility = Visibility.Visible;
            pauseButtonSymbol.Visibility = Visibility.Collapsed;
        }

        public void StopPlaying()
        {
            media.Stop();
            SwitchToPlaySymbol();
            IsPlaying = false;
        }

        private async Task StartMediaSource(string text)
        {
            if (textToSynth == null || !String.Equals(text, textToSynth, StringComparison.OrdinalIgnoreCase))
            {
                textToSynth = text;
                SynthesisStreamDispose();
                //GC.Collect(); //Disable in Creator Update

                synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(text);                
                media.SetSource(synthesisStream, synthesisStream.ContentType);
            }            
            media.Play();
        }

        private async Task UnvailableMediaPlayerExceptionHandler()
        {
            SwitchToPlaySymbol();
            playButton.IsEnabled = false;
            listboxVoiceChooser.IsEnabled = false;
            await UIHelper.ShowMessageDialog("Media player components are unavailable");
        }

        private async Task UnableToSynthExceptionHandler()
        {
            SwitchToPlaySymbol();
            media.AutoPlay = false;
            await UIHelper.ShowMessageDialog("Unable to synthesize card content");
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            IsPlaying = false;
            SwitchToPlaySymbol();
        }

        private void InitializeListboxVoiceChooser()
        {            
            var voices = SpeechSynthesizer.AllVoices;
            VoiceInformation currentVoice = synthesizer.Voice;

            foreach (VoiceInformation voice in voices.OrderBy(p => p.Language))
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Name = voice.DisplayName;
                item.Tag = voice;                
                item.Content = voice.DisplayName + " (Language: " + voice.Language + ")";
                listboxVoiceChooser.Items.Add(item);

                // Check to see if we're looking at the current voice and set it as selected in the listbox.
                if (currentVoice.Id == voice.Id)
                {
                    item.IsSelected = true;
                    listboxVoiceChooser.SelectedItem = item;
                }
            }
        }

        public void ChangeVoice(string voiceId)
        {
            foreach(var item in listboxVoiceChooser.Items)
            {
                ComboBoxItem comItem = item as ComboBoxItem;
                if (comItem == null)
                    continue;

                VoiceInformation voice = comItem.Tag as VoiceInformation;
                if (voice == null)
                    continue;

                if (voice.Id.Equals(voiceId, StringComparison.OrdinalIgnoreCase))
                {
                    isSuppressVoiceChangedEvent = true;
                    listboxVoiceChooser.SelectedItem = item;
                    return;
                }
            }
        }

        private void OnListboxVoiceChooserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Set this to null so the same text can be re-synthesized
            textToSynth = null;
            SetVoice();

            if (isSuppressVoiceChangedEvent)
                isSuppressVoiceChangedEvent = false;
            else
                VoiceChanged?.Invoke(synthesizer.Voice.Id, e);
        }

        public void SetVoice()
        {
            ComboBoxItem item = listboxVoiceChooser.SelectedItem as ComboBoxItem;
            if (item == null)
                return;

            VoiceInformation voice = item.Tag as VoiceInformation;
            if (voice == null)
                return;

            synthesizer.Voice = voice;
        }

        public void Dispose()
        {
            StopPlaying();
            MediaDispose();
            SynthesizerDispose();
            SynthesisStreamDispose();            
            //GC.Collect(); //Disable in Creator Update
        }

        private void MediaDispose()
        {
            if (media.Source != null)
            {
                media.Source = null;
            }
        }

        private void SynthesisStreamDispose()
        {
            if (synthesisStream != null)
            {
                synthesisStream.Dispose();
                synthesisStream = null;
            }
        }

        private void SynthesizerDispose()
        {
            if (synthesizer != null)
            {
                synthesizer.Dispose();
                synthesizer = null;
            }
        }

        public void ChangePlayBackRate(double rate)
        {
            try
            {
                if (rate < MIN_PLAYBACK_RATE)
                    rate = 0.25;
                else if (rate > MAX_PLAYBACK_RATE)
                    rate = 2;

                media.DefaultPlaybackRate = rate;
                media.PlaybackRate = media.DefaultPlaybackRate;
                
                ChangePlayBackRateSlider(rate);                
                ChangePlayBackChooser(rate);
            }
            catch
            { }
        }

        private void ChangePlayBackRateSlider(double rate)
        {
            playBackRateSlider.Value = rate;            
        }

        private void ChangePlayBackChooser(double rate)
        {
            try
            {
                foreach (var item in playBackRateChooser.Items)
                {
                    var combItem = item as ComboBoxItem;
                    if (combItem == null)
                        continue;

                    var value = Convert.ToDouble(combItem.Tag);
                    if (value == rate)
                    {                        
                        playBackRateChooser.SelectedItem = item;
                        return;
                    }
                }
            }
            catch
            { }
        }

        private void OnPlayBackRateChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = playBackRateChooser.SelectedItem as ComboBoxItem;
            if (item == null)
                return;
            try
            {
                var rate = Convert.ToDouble(item.Tag);
                media.DefaultPlaybackRate = rate;
                media.PlaybackRate = media.DefaultPlaybackRate;

                FireRateChangeEventIfNeeded(rate);
            }
            catch
            {  }
        }

        private void OnPlaybackRateSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            try
            {
                if (playBackRateSlider.Value < MIN_PLAYBACK_RATE)
                    playBackRateSlider.Value = MIN_PLAYBACK_RATE;

                if(playBackSliderLabel != null)
                    playBackSliderLabel.Text = playBackRateSlider.Value + "x";
                media.DefaultPlaybackRate = playBackRateSlider.Value;
                media.PlaybackRate = media.DefaultPlaybackRate;

                FireRateChangeEventIfNeeded(playBackRateSlider.Value);
            }
            catch
            { }            
        }

        private void FireRateChangeEventIfNeeded(double rate)
        {
            PlayBackRateChanged?.Invoke(rate);
        }

        private void OnUserControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(userControl.ActualWidth >= 400)
            {                
                if (!isNarrowState)
                    return;

                isNarrowState = false;
                playBackRateSlider.Visibility = Visibility.Visible;
                playBackSliderLabel.Visibility = Visibility.Visible;
                playBackRateChooser.Visibility = Visibility.Collapsed;
                playBackGridColumn.Width = new GridLength(1, GridUnitType.Star);
                ChangePlayBackRateSlider(media.DefaultPlaybackRate);
            }
            else
            {
                if (isNarrowState)
                    return;

                isNarrowState = true;                
                playBackRateSlider.Visibility = Visibility.Collapsed;
                playBackSliderLabel.Visibility = Visibility.Collapsed;
                playBackRateChooser.Visibility = Visibility.Visible;
                playBackGridColumn.Width = new GridLength(0, GridUnitType.Auto);
                ChangePlayBackChooser(media.DefaultPlaybackRate);
            }
        }
    }
}
