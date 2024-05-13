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

using AnkiU.Models;
using AnkiU.UIUtilities;
using AnkiU.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace AnkiU.Views
{
    public sealed partial class TagInformationView : UserControl
    {
        public string Label { get { return label.Text; } set { label.Text = value; } }
        public Visibility LabelVisibility { get { return label.Visibility; } set { label.Visibility = value; } }
        public FlyoutPlacementMode Placement { get { return tagsViewFlyout.Placement; } set { tagsViewFlyout.Placement = value; } }

        public event EventHandler TagFlyoutClosedEvent;

        public TagInformationViewModel ViewModel
        {
            get { return (TagInformationViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(TagInformationViewModel), typeof(TagInformationView), new PropertyMetadata(null));

        public TagInformationView()
        {
            this.InitializeComponent();
        }

        public Visibility AddVisibility
        {
            get { return (Visibility)GetValue(AddVisibilityProperty); }
            set { SetValue(AddVisibilityProperty, value); }
        }
        
        public static readonly DependencyProperty AddVisibilityProperty =
            DependencyProperty.Register("AddVisibility", typeof(Visibility), typeof(TagInformationView), new PropertyMetadata(Visibility.Visible));


        private async void ExpandTagButtonClickHandler(object sender, RoutedEventArgs e)
        {
            if(ViewModel.Tags.Count == 0)
            {
                string message = "You do not have any tags yet.";
                if (AddVisibility == Visibility.Visible)
                    message += "\nTo add new tags please press on the \"Add\" icon.";

                await UIHelper.ShowMessageDialog(message);
                return;
            }
            searchTextBox.Text = "";
            if(expandTagButton.ActualWidth < tagsNameGrid.MaxWidth)
                tagsNameGrid.Width = expandTagButton.ActualWidth;
            else
                tagsNameGrid.Width = tagsNameGrid.MaxWidth;

            tagsNameGrid.MaxHeight = CoreWindow.GetForCurrentThread().Bounds.Height / 2;
            tagsViewFlyout.ShowAt(expandTagButton);            
        }

        private void TagsViewFlyoutClosedHandler(object sender, object e)
        {
            ViewModel.UpdateNoteTagsFromField();
            TagFlyoutClosedEvent?.Invoke(sender, null);
        }

        private void NewTagButtonClickHandler(object sender, RoutedEventArgs e)
        {
            newTagFlyoutTextBox.Text = "";
            newTagFlyout.ShowAt(newTagButton);
        }

        private void NewTagCancelButtonClickHandler(object sender, RoutedEventArgs e)
        {
            newTagFlyout.Hide();
        }

        private void NewTagFlyoutOKButtonClickHandler(object sender, RoutedEventArgs e)
        {
            ViewModel.AddNewTags(newTagFlyoutTextBox.Text);
            newTagFlyout.Hide();
        }

        private async void NewTagFlyoutTextBoxEnterKeyDownHandler(object sender, KeyRoutedEventArgs e)
        {
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    NewTagFlyoutOKButtonClickHandler(sender, null);
                    e.Handled = true;
                }
            });
        }

        private void SearchTextBoxTextChangedHandler(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(searchTextBox.Text))
            {
                foreach (var item in allTagsView.Items)
                {
                    var tag = item as TagInformation;
                    tag.Visibility = Visibility.Visible;
                }
                return;
            }

            foreach (var item in allTagsView.Items)
            {
                var tag = item as TagInformation;
                if(tag.Name.ToUpperInvariant().Contains(searchTextBox.Text.ToUpperInvariant()))
                    tag.Visibility = Visibility.Visible;
                else
                    tag.Visibility = Visibility.Collapsed;
            }

        }
    }
}
