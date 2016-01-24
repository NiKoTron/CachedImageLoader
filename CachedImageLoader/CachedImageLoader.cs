/*
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using Android.Content;
using Android.Graphics;
using System.Net;
using Android.App;
using System.IO;
using Android.Widget;
using Android.Util;
using System.Security.Cryptography;
using Java.Util;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace co.littlebyte.Utils
{
	public class CachedImageLoader
	{
		private static string TAG = "CachedImageLoader";

		public event EventHandler<Bitmap> OnLoad;
		public event EventHandler<Exception> OnException;

		public BitmapFactory.Options BitmapOptions { get; set; }

		private readonly string _url;

		private static Dictionary<string,Bitmap> _staticCache = new Dictionary<string,Bitmap> ();

		public CachedImageLoader (string url)
		{
			_url = url;
		}

		/*
		private static string GetMD5 (string s)
		{
			MD5 md5 = MD5.Create ();
			var encoding = new System.Text.UnicodeEncoding ();

			byte[] bytes = encoding.GetBytes (s); 
			byte[] result = md5.ComputeHash (bytes);

			var res = System.Text.Encoding.Unicode.GetString (result);

			return res;
		}
		*/

		public static string GetMD5 (string str)
		{
			try {
				var s = new Java.Lang.String (str);

				// Create MD5 Hash
				var digest = Java.Security.MessageDigest.GetInstance ("MD5");
				digest.Update (s.GetBytes ());
				byte[] messageDigest = digest.Digest ();

				// Create Hex String
				Java.Lang.StringBuffer hexString = new Java.Lang.StringBuffer ();
				for (int i = 0; i < messageDigest.Length; i++) {
					String h = Java.Lang.Integer.ToHexString (0xFF & messageDigest [i]);
					while (h.Length < 2)
						h = "0" + h;
					hexString.Append (h);
				}
				return hexString.ToString ();

			} catch (Java.Security.NoSuchAlgorithmException ex) {
				Log.Error (TAG, ex.StackTrace);
			}
			return "";
		}

		public void Load (BitmapFactory.Options o = null, int scaleWidth = -1, int scaleHeight = -1)
		{
			Bitmap bmp = null;
			var uri = new Uri (_url);
			var client = new WebClient ();

			string url_hash = GetMD5 (_url);

			var path = Application.Context.CacheDir.AbsolutePath + "/" + url_hash;
			var f_info = new FileInfo (path);

			if (o == null) {
				o = new BitmapFactory.Options (){ InPurgeable = true };
			}

			if (_staticCache.ContainsKey (url_hash)) {
				if (OnLoad != null) {
					OnLoad (this, _staticCache [url_hash]);
				}
				return;
			}

			if (f_info.Exists) {
				
				Task task = new Task (() => {
					
					var ba = File.ReadAllBytes (path);

					bmp = BitmapFactory.DecodeByteArray (ba, 0, ba.Length, o);

					_staticCache [url_hash] = bmp;

					if (OnLoad != null) {
						OnLoad (this, bmp);
					}

				});
				task.Start ();

			} else {
				client.DownloadDataCompleted += async (sender, e) => {
					if (!e.Cancelled && e.Error == null) {
						if (e.Result != null && e.Result.Length > 0) {
							bmp = await BitmapFactory.DecodeByteArrayAsync (e.Result, 0, e.Result.Length, o);

							if (scaleWidth > 0 && scaleHeight > 0) {
								bmp = Bitmap.CreateScaledBitmap (bmp, scaleWidth, scaleHeight, true);
							}
								
							Task task = new Task (() => {

								f_info.Directory.Create ();

								var fileStream = new FileStream (path, 
									                 FileMode.OpenOrCreate, 
									                 FileAccess.ReadWrite, 
									                 FileShare.None);

								var compress = bmp.Compress (Bitmap.CompressFormat.Png, 100, fileStream);

								fileStream.Close ();

								_staticCache [url_hash] = bmp;
							
								if (OnLoad != null) {
									OnLoad (this, bmp);
								}

							});
							task.Start ();

						}
					}
				};
				try {
					client.DownloadDataAsync (uri);
				} catch (WebException ex) {
					Log.Error (TAG, ex.StackTrace);
					var eh = OnException;

					if (eh != null) {
						OnException (this, ex);
					}
				}
			}
		}


		public static void LoadImageFromUrl (ImageView iv, string url, BitmapFactory.Options options, int scaleWidth = -1, int scaleHeight = -1)
		{
			var l = new CachedImageLoader (url);

			l.OnLoad += (sender, e) => {
				if (e != null) {
					iv.SetImageBitmap (e);
				}
			};

			l.Load (options, scaleWidth, scaleHeight);
		}
	}
}

