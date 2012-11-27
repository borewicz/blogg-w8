using Blogg.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Szablon elementu Kontrakt funkcji Wyszukaj jest udokumentowany pod adresem http://go.microsoft.com/fwlink/?LinkId=234240

namespace Blogg
{
    /// <summary>
    /// Ta strona wyświetla wyniki wyszukiwania, kiedy globalne wyszukiwanie jest skierowane na tę aplikację.
    /// </summary>
    public sealed partial class SearchResultsPage : Blogg.Common.LayoutAwarePage
    {
        private SearchPane searchPane;

        public SearchResultsPage()
        {
            this.InitializeComponent();
            //this.searchPane = SearchPane.GetForCurrentView();
            //this.searchPane.SuggestionsRequested += searchPane_SuggestionsRequested;
        }

        /// <summary>
        /// Wypełnia stronę zawartością przekazywaną podczas nawigowania. Dowolny zapisany stan jest również
        /// dostarczany podczas odtwarzania strony z poprzedniej sesji.
        /// </summary>
        /// <param name="navigationParameter"> Wartość parametru przekazywana do
        /// <see cref="Frame.Navigat(Type, Object)"/> , gdy tej strony żądano po raz pierwszy.
        /// </param>
        /// <param name="pageState">Słownik stanu zachowanego przez tą stronę podczas wcześniejszej
        /// sesji. Strona będzie pusta podczas pierwszej wizyty. </param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            var queryText = navigationParameter as String;

            // TODO: Logika wyszukiwania specyficzna dla aplikacji. Proces wyszukiwania jest odpowiedzialny za
            //       stworzenie listy wybieralnych przez użytkownika kategorii wyników:
            //
            //       metodzie filterList.Add(new Filter("<filter name>", <result count>));
            //
            //       Tylko pierwszy filtr, zazwyczaj "All", powinien przekazać wartość true jako trzeci argument w
            //       aby uruchamiać w stanie aktywnym.  Wyniki dla filtra aktywnego są dostarczane
            //       w metodzie Filter_SelectionChanged poniżej.

            var group = (from gr in App.dataSource.blogs
                         from i in gr.Items
                         //where i.name.Contains(queryText)
                         where i.name.IndexOf(queryText, StringComparison.OrdinalIgnoreCase) >= 0
                         select i);

            this.DefaultViewModel["Results"] = group.ToList();
            var filterList = new List<Filter>();
            filterList.Add(new Filter("All", 0, true));

            // Przekaż wyniki za pośrednictwem modelu widoku
            this.DefaultViewModel["QueryText"] = '\u201c' + queryText + '\u201d';
            this.DefaultViewModel["Filters"] = filterList;
            this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;
        }

        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Navigate to the appropriate destination page, configuring the new page
            // by passing required information as a navigation parameter
            var itemId = ((PostItem)e.ClickedItem).id;
            this.Frame.Navigate(typeof(ItemDetailPage), itemId);
        }

        //void searchPane_SuggestionsRequested(SearchPane sender, SearchPaneSuggestionsRequestedEventArgs args)
        //{
        //    var suggestions = (from gr in App.dataSource.blogs
        //                       from i in gr.Items
        //                       //where i.name.Contains(queryText)
        //                       select i.name);

        //    foreach (string suggestion in suggestions)
        //    {
        //        if (suggestion.StartsWith(args.QueryText, StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            args.Request.SearchSuggestionCollection.AppendQuerySuggestion(suggestion);
        //        }

        //        if (args.Request.SearchSuggestionCollection.Size >= 5)
        //        {
        //            break;
        //        }
        //    }
        //}

        /// <summary>
        /// Wywoływane, gdy filtr jest wybrany przy użyciu pola kombi w przypiętym stanie widoku.
        /// </summary>
        /// <param name="sender">Wystąpienie pola kombi.</param>
        /// <param name="e">Dane zdarzenia opisujące, jak zmieniony był wybrany filtr.</param>
        void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Określi jaki filtr został wybrany
            var selectedFilter = e.AddedItems.FirstOrDefault() as Filter;
            if (selectedFilter != null)
            {
                // Duplikuj wyniki do odpowiadającego im obiektu filtra, aby zezwolić
                // Reprezentacja elementu RadioButton używana, gdy nie jest przypięty, aby odzwierciedlić zmianę
                selectedFilter.Active = true;

                // TODO: Odpowiedz na zmianę filtra aktywnego ustawiając właściwość this.DefaultViewModel["Results"]
                //    zbieranie elementów mających właściwości Image, Title, Subtitle i Description, których można używać w powiązaniach

                // Upewnienie, że zostały znalezione wyniki
                object results;
                ICollection resultsCollection;
                if (this.DefaultViewModel.TryGetValue("Results", out results) &&
                    (resultsCollection = results as ICollection) != null &&
                    resultsCollection.Count != 0)
                {
                    VisualStateManager.GoToState(this, "ResultsFound", true);
                    return;
                }
            }

            // Wyświetl tekst informacyjny, gdy nie będą dostępne wyniki wyszukiwania.
            VisualStateManager.GoToState(this, "NoResultsFound", true);
        }

        /// <summary>
        /// Wywoływane, gdy filtr jest zaznaczony przy użyciu elementu RadioButton, kiedy nie jest przypięty.
        /// </summary>
        /// <param name="sender">Wybrane wystąpienie formantu RadioButton.</param>
        /// <param name="e">Dane zdarzenia, opisujące, jak wybrany został element RadioButton.</param>
        void Filter_Checked(object sender, RoutedEventArgs e)
        {
            // Duplikuj zmianę do klasy CollectionViewSource, używanej przez odpowiadające pole kombi
            // aby zapewnić, że zmiana jest odzwierciedlona, gdy jest przypięty
            if (filtersViewSource.View != null)
            {
                var filter = (sender as FrameworkElement).DataContext;
                filtersViewSource.View.MoveCurrentTo(filter);
            }
        }

        /// <summary>
        /// Model widoku, opisujący jeden z dostępnych filtrów do wyświetlania wyników wyszukiwania.
        /// </summary>
        private sealed class Filter : Blogg.Common.BindableBase
        {
            private String _name;
            private int _count;
            private bool _active;

            public Filter(String name, int count, bool active = false)
            {
                this.Name = name;
                this.Count = count;
                this.Active = active;
            }

            public override String ToString()
            {
                return Description;
            }

            public String Name
            {
                get { return _name; }
                set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
            }

            public int Count
            {
                get { return _count; }
                set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
            }

            public bool Active
            {
                get { return _active; }
                set { this.SetProperty(ref _active, value); }
            }

            public String Description
            {
                get { return String.Format("{0} ({1})", _name, _count); }
            }
        }
    }
}
