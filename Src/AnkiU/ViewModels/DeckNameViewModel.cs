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

namespace AnkiU.ViewModels
{
    public class DeckNameViewModel
    {
        public const int ALL_DECKS_ID = -1;

        public ObservableCollection<DeckInformation> Decks;

        public int TotalNumberOfCards { get; set; }

        public DeckNameViewModel(Collection collection, bool isIncludeAllDeck = true, bool isIncludeDynamicDeck = true)
        {
            var deckList = collection.Deck.All();
            List<DeckInformation> temp = new List<DeckInformation>();

            foreach (var deck in deckList)
            {
                long did = (long)JsonHelper.GetNameNumber(deck,"id");
                //Previously, AnkiU hide default deck in all cases.
                //Since we start to support AnkiWeb, we'll have to show default deck if it has cards to review                
                if (did == Constant.DEFAULTDECK_ID)
                {
                    if (collection.CardCount(did) < 1)
                        continue;
                }

                string name = deck.GetNamedString("name");

                bool isDynamic = collection.Deck.IsDyn(did);
                if (!isIncludeDynamicDeck && isDynamic)
                    continue;
                
                temp.Add(new DeckInformation(name, 0, 0, did, isDynamic));
            }
            temp.Sort((a, b) =>
            {
                return a.BaseName.CompareTo(b.BaseName);
            });


            if (isIncludeAllDeck)
                temp.Insert(0, new DeckInformation("All decks", 0, 0, ALL_DECKS_ID, false));

            Decks = new ObservableCollection<DeckInformation>(temp);
        }

    }
}
