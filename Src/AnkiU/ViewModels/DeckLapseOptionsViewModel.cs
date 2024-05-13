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

using AnkiU.AnkiCore;
using AnkiU.Interfaces;
using AnkiU.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace AnkiU.ViewModels 
{
    public class DeckLapseOptionsViewModel : IAnkiDeckOptionsViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private JsonObject config;
        public JsonObject Config
        {
            get { return config; }
            set
            {
                config = value;
                RaisePropertyChanged("Config");
            }
        }

        private DeckLapseOptions options;
        public DeckLapseOptions Options
        {
            get { return options; }
            set
            {
                options = value;
                RaisePropertyChanged("Config");
            }
        }

        public DeckLapseOptionsViewModel(JsonObject config)
        {
            this.Config = config.GetNamedObject("lapse");
            Options = new DeckLapseOptions();
        }        

        public void GetOptionsToView()
        {
            try
            {
                Options.Delays = Utils.JsonNumberArrayToString(Config.GetNamedArray("delays"));
                Options.NewInterval = (int)(JsonHelper.GetNameNumber(Config,"mult") * 100);
                Options.MinInt = (int)JsonHelper.GetNameNumber(Config,"minInt");
                Options.LeechFailsThreshold = (int)JsonHelper.GetNameNumber(Config,"leechFails");
                Options.LeechAction = (int)JsonHelper.GetNameNumber(Config,"leechAction");                
            }
            catch //If any error happen we back to default
            {
                Options = new DeckLapseOptions();
            }            
        }

        public void SaveOptionsToJsonConfig()
        {                                   
            Config["delays"] = Utils.StringNumberToJsonArray(Options.Delays);
            Config["mult"] = JsonValue.CreateNumberValue(Options.NewInterval / 100.0);
            Config["minInt"] = JsonValue.CreateNumberValue(Options.MinInt);
            Config["leechFails"] = JsonValue.CreateNumberValue(Options.LeechFailsThreshold);
            Config["leechAction"] = JsonValue.CreateNumberValue(Options.LeechAction);
        }

        
    }
}
