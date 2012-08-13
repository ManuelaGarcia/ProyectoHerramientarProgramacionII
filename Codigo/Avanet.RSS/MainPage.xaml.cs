using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Xml;
using System.ServiceModel.Syndication;
using System.IO;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace Avanet.RSS
{
	public partial class MainPage : PhoneApplicationPage
	{
		ProgressIndicator barraProgreso;

		// Constructor
		public MainPage()
		{
			InitializeComponent();

			this.DataContext = App.VistaModelo;

			this.Loaded += new RoutedEventHandler(MainPage_Loaded);

			this.barraProgreso = new ProgressIndicator()
			{
				IsIndeterminate = true,
				Text = "Descargando RSS.."
			};

			SystemTray.SetProgressIndicator(this, barraProgreso);
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			if (NavigationContext.QueryString.ContainsKey("enlace"))
			{
				WebBrowserTask lanzador = new WebBrowserTask();
				lanzador.Uri = new Uri(NavigationContext.QueryString["enlace"].ToString(), UriKind.Absolute);
				lanzador.Show();
			}
		}

		void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			CargarRSS();
            VerificarBaseDatos();
			CargarFavoritos();
		}

		private void VerificarBaseDatos()
		{
			using (FavoritosDataContext contexto = new FavoritosDataContext(App.CadenaConexion))
			{
				if (!contexto.DatabaseExists())
				{
					contexto.CreateDatabase();
				}
			}
		}

		private void CargarFavoritos()
		{
			using (FavoritosDataContext contexto = new FavoritosDataContext(App.CadenaConexion))
			{
				ObservableCollection<Favorito> resultado = new ObservableCollection<Favorito>();

				(from f in contexto.Favoritos select f).ToList().ForEach(x =>
					resultado.Add(x));

				App.VistaModelo.Favoritos = resultado;
			}
		}

		private void CargarRSS()
		{
			this.barraProgreso.IsVisible = true;

			WebClient cliente = new WebClient();
			cliente.DownloadStringAsync(new Uri("http://avanet.org/blog924rss.aspx"
				+ "?Trick=" + DateTime.Now.ToShortDateString() + DateTime.Now.ToShortTimeString()));

			cliente.DownloadStringCompleted += new DownloadStringCompletedEventHandler(cliente_DownloadStringCompleted);
		}

		void cliente_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			XmlReader reader = XmlReader.Create(new StringReader(e.Result));
			SyndicationFeed feed = SyndicationFeed.Load(reader);
			this.pivTodos.DataContext = feed;

			barraProgreso.IsVisible = false;
		}

		private void btnActualizar_Click(object sender, System.EventArgs e)
		{
			CargarRSS();
		}

		private void mnuCompartirEnlace_Click(object sender, RoutedEventArgs e)
		{
			System.ServiceModel.Syndication.SyndicationItem item = (sender as FrameworkElement).DataContext 
				as System.ServiceModel.Syndication.SyndicationItem;

			if (item != null)
			{
				//1. Instanciar el lanzador
				ShareLinkTask lanzador = new ShareLinkTask();
				//2. Configurar las propiedades
				lanzador.LinkUri = new Uri(item.Links[0].Uri.AbsoluteUri);
				lanzador.Message = string.Format(item.Summary.Text.Substring(0,50) + "...");
				lanzador.Title = "Post de @avanet " + item.Title.Text;
				//3. Mostrar el lanzador
				lanzador.Show();
			}
		}


		public SyndicationItem itemSeleccionado { get; set; }

		private void mnuEnviarCorreo_Click(object sender, RoutedEventArgs e)
		{
			 System.ServiceModel.Syndication.SyndicationItem item = (sender as FrameworkElement).DataContext 
							as System.ServiceModel.Syndication.SyndicationItem;

			 if (item != null)
			 {
				 //1. Instanciamos el selector
				 EmailAddressChooserTask selector = new EmailAddressChooserTask();
				 //2. Completamos las propiedades si las tiene (En este caso no las tiene)
				 //3. Nos suscribimos al evento Completed
				 selector.Completed += new EventHandler<EmailResult>(selector_Completed);
				 this.itemSeleccionado = item;
				 //4. Mostramos el selector
				 selector.Show();
			 }
		}

		void selector_Completed(object sender, EmailResult e)
		{
            if (e.TaskResult == TaskResult.OK)
            {
                EmailComposeTask lanzador = new EmailComposeTask();
                lanzador.To = e.Email;
                lanzador.Body = string.Format("Te comparto el post \"{0}\" que leí " +
                    "a través del RSS de Avanet\r\n Enlace: {1}",
                    itemSeleccionado.Title.Text, itemSeleccionado.Links[0].Uri.AbsoluteUri);
                lanzador.Show();
            }
		}

		private void mnuGuardarFavorito_Click(object sender, RoutedEventArgs e)
		{
            System.ServiceModel.Syndication.SyndicationItem item = (sender as FrameworkElement).DataContext
                                as System.ServiceModel.Syndication.SyndicationItem;

            using (FavoritosDataContext contexto = new FavoritosDataContext(App.CadenaConexion))
            {
                Favorito favoritoConsultado = (from f in contexto.Favoritos
                                               where f.Identificador.Equals(item.Id)
                                               select f).SingleOrDefault();

                if (favoritoConsultado == null)
                {
                    Favorito nuevoFavorito = new Favorito()
                    {
                        Identificador = item.Id,
                        Enlace = item.Links[0].Uri.AbsoluteUri,
                        Resumen = item.Summary.Text.Substring(50),
                        Titulo = item.Title.Text
                    };

                    contexto.Favoritos.InsertOnSubmit(nuevoFavorito);
                    contexto.SubmitChanges();
                }
                else
                    MessageBox.Show("El favorito ya existe");
            }

            CargarFavoritos();
		}

		private void mnuEliminarFavorito_Click(object sender, RoutedEventArgs e)
		{
			Favorito item = (sender as FrameworkElement).DataContext as Favorito;

			using (FavoritosDataContext contexto = new FavoritosDataContext(App.CadenaConexion))
			{
				Favorito favoritoConsultado = (from f in contexto.Favoritos
													where f.Identificador.Equals(item.Identificador)
													select f).SingleOrDefault();

				if (favoritoConsultado != null)
				{
					contexto.Favoritos.DeleteOnSubmit(favoritoConsultado);
					contexto.SubmitChanges();
				}
			}

            CargarFavoritos();
		}

		private void mnuAnclarInicioFavorito_Click(object sender, RoutedEventArgs e)
		{
			Favorito item = (sender as FrameworkElement).DataContext
			   as Favorito;

			StandardTileData nuevoMosaico = new StandardTileData
			{
				BackgroundImage = new Uri("Background.png", UriKind.Relative),
				Title = "Favorito Avanet",
				BackContent = item.Titulo,
			};

			ShellTile.Create(new Uri("/MainPage.xaml?enlace=" + item.Enlace, UriKind.Relative), nuevoMosaico);
		}
	}
}