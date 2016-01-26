using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using System.Collections.Generic;
using System;
using Android.Content;
using co.littlebyte.Utils;

namespace CachedImageLoaderApp
{


	[Activity (Label = "CachedImageLoaderApp", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		int count = 1;

		List<Tuple<string,string>> MyList = new List<Tuple<string, string>> () {
			new Tuple<string, string> (@"http://is5.mzstatic.com/image/thumb/Purple/v4/56/50/9d/56509db5-e69c-06f9-aeb5-6eb07dbc1980/source/512x512sr.jpg", "item1"),
			new Tuple<string, string> (@"https://dlwgolf.files.wordpress.com/2014/08/coffee-206142_640.jpg", "item2"),
			new Tuple<string, string> (@"http://online.thatsmags.com/uploads/content/1412/7025/general-siberian-husky-in-anger-wallpaper-siberian-husky-wallpaper.jpg", "item3"),
			new Tuple<string, string> (@"http://img2.goodfon.su/wallpaper/big/8/74/rys-koshka-tambako-the-jaguar.jpg", "item4"),
			new Tuple<string, string> (@"https://dlwgolf.files.wordpress.com/2014/08/coffee-206142_640.jpg", "item5 = item2"),
			new Tuple<string, string> (@"http://cdn.images.express.co.uk/img/dynamic/128/590x/space-alien-sighting-573293.jpgh", "item6"),
			new Tuple<string, string> (@"https://airbusdefenceandspace.com/wp-content/uploads/2016/01/satellite_image_worlddem_guelb_er_richat_mauritania-2.jpg", "item7"),
			new Tuple<string, string> (@"http://www.thinkstockphotos.com/CMS/StaticContent/Hero/TS_AnonHP_462882495_01.jpg", "item8"),
			new Tuple<string, string> (@"https://dlwgolf.files.wordpress.com/2014/08/coffee-206142_640.jpg", "item9 = item2"),
			new Tuple<string, string> (@"https://amazingslider.com/wp-content/uploads/2012/12/dandelion.jpg", "item10")
		};

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
			
			button.Click += delegate {
				button.Text = string.Format ("{0} clicks!", count++);
			};

			var rv = FindViewById<RecyclerView> (Resource.Id.recycler_view);
			rv.SetLayoutManager (new LinearLayoutManager (this));

			rv.SetAdapter (new Adaptor (MyList));

		}

		class Adaptor : RecyclerView.Adapter
		{
			class ViewHolder : RecyclerView.ViewHolder
			{
				public ImageView Icon { get; set; }

				public TextView Title { get; set; }

				public ViewHolder (View itemView) : base (itemView)
				{
					Icon = itemView.FindViewById<ImageView> (Resource.Id.image_view);
					Title = itemView.FindViewById<TextView> (Resource.Id.text_view);
				}
			}

			List<Tuple<string,string>> _items;

			public Adaptor (List<Tuple<string,string>> items) : base ()
			{
				_items = items;
			}

			#region implemented abstract members of Adapter

			public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
			{
				var vh = holder as ViewHolder;
				vh.Title.Text = _items [position].Item2;
				co.littlebyte.Utils.CachedImageLoader.LoadImageFromUrl (vh.Icon, _items [position].Item1, null, 12, 12);

			}

			public override RecyclerView.ViewHolder OnCreateViewHolder (Android.Views.ViewGroup parent, int viewType)
			{
				var inflater = (LayoutInflater)Application.Context.GetSystemService (Context.LayoutInflaterService);
				var view = inflater.Inflate (Resource.Layout.item_recyler_item, null);
				var viewHolder = new ViewHolder (view);
				return viewHolder;

			}

			public override int ItemCount {
				get {
					return _items.Count;
				}
			}

			#endregion
		}
	}
}


