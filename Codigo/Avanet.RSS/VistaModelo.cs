using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Avanet.RSS
{
    public class VistaModelo : INotifyPropertyChanged
    {
        private ObservableCollection<Favorito> favoritos = new ObservableCollection<Favorito>();

        public ObservableCollection<Favorito> Favoritos
        {
            get { return favoritos; }
            set 
            { 
                favoritos = value;
                OnPropertyChanged("Favoritos");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propName)
        {
            PropertyChangedEventHandler eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
