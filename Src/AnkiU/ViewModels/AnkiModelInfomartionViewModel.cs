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
using AnkiU.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;

namespace AnkiU.ViewModels
{
    public class AnkiModelInfomartionViewModel
    {
        public int CurrentModelIndex { get; set; }
        public ObservableCollection<AnkiModelInformation> Models { get; set; }

        public AnkiModelInfomartionViewModel(IEnumerable<JsonObject> models)
        {
            List<AnkiModelInformation> temp = new List<AnkiModelInformation>();
            foreach(var model in models)
            {
                string name = model.GetNamedString("name");
                long id = (long)JsonHelper.GetNameNumber(model,"id");                
                AnkiModelInformation m = new AnkiModelInformation(name, id);
                temp.Add(m);
            }
            temp.Sort((x, y) => { return x.Name.CompareTo(y.Name); });
            this.Models = new ObservableCollection<AnkiModelInformation>(temp);
        }
        
    }
}