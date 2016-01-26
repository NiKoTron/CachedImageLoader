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
using Android.Graphics;
using System.Net;
using Android.App;
using System.IO;
using Android.Widget;
using Android.Util;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace co.littlebyte.Utils
{
	public class BitmapCompressOptions
	{
		public Bitmap.CompressFormat Format = Bitmap.CompressFormat.Png;
		public int Quality = 100;
	}

	public class Bob
	{
		public Bitmap Bitmap { get; set; }
		 
	}

	public class CachedImageLoader
	{
		private static readonly string TAG = "CachedImageLoader";

		public event EventHandler<Bitmap> OnLoad;
		public event EventHandler<Exception> OnException;

		public BitmapFactory.Options BitmapOptions { get; set; }

		private readonly string _url;

		private static readonly Dictionary<string, Bitmap> _staticRAMCache = new Dictionary<string, Bitmap> ();

		public static void ClearRAMCache ()
		{
			_staticRAMCache.Clear ();
		}

		public CachedImageLoader (string url)
		{
			_url = url;
		}

		public static string GetMD5 (Java.Lang.String s)
		{
			try {
				// Create MD5 Hash
				var digest = Java.Security.MessageDigest.GetInstance ("MD5");
				digest.Update (s.GetBytes ());
				var messageDigest = digest.Digest ();

				// Create Hex String
				var hexString = new Java.Lang.StringBuffer ();
				for (int i = 0; i < messageDigest.Length; i++) {
					var h = Java.Lang.Integer.ToHexString (0xFF & messageDigest [i]);
					while (h.Length < 2) {
						h = "0" + h;
					}
					hexString.Append (h);
				}
				return hexString.ToString ();

			} catch (Java.Security.NoSuchAlgorithmException ex) {
				Log.Error (TAG, ex.StackTrace);
			}
			return "";
		}

		public static int CalculateInSampleSize (BitmapFactory.Options options, int reqWidth, int reqHeight)
		{
			// Raw height and width of image
			int height = options.OutHeight;
			int width = options.OutWidth;
			int inSampleSize = 1;

			if (height > reqHeight || width > reqWidth) {

				int halfHeight = height / 2;
				int halfWidth = width / 2;

				// Calculate the largest inSampleSize value that is a power of 2 and keeps both
				// height and width larger than the requested height and width.
				while ((halfHeight / inSampleSize) > reqHeight
				       && (halfWidth / inSampleSize) > reqWidth) {
					inSampleSize *= 2;
				}
			}

			return inSampleSize;
		}

		public void Load (BitmapFactory.Options o = null, int scaleWidth = -1, int scaleHeight = -1, BitmapCompressOptions compressOptions = null)
		{
			Bitmap bmp = null;

			if (compressOptions == null) {
				compressOptions = new BitmapCompressOptions ();
			}

			var uri = new Uri (_url);
			var client = new WebClient ();

			var urlHash = GetMD5 (new Java.Lang.String (string.Format ("{0}{1}{2}", _url, scaleWidth, scaleHeight)));

			var path = Application.Context.CacheDir.AbsolutePath + "/" + urlHash;
			var fileInfo = new FileInfo (path);

			if (o == null) {
				o = new BitmapFactory.Options () { InPurgeable = true };
			}

			if (_staticRAMCache.ContainsKey (urlHash)) {
				if (OnLoad != null) {
					OnLoad (this, _staticRAMCache [urlHash]);
				}
				return;
			}

			if (fileInfo.Exists) {

				var task = new Task (() => {
					var ba = File.ReadAllBytes (path);
					bmp = BitmapFactory.DecodeByteArray (ba, 0, ba.Length, o);

					_staticRAMCache [urlHash] = bmp;

					if (OnLoad != null) {
						OnLoad.Invoke (this, bmp);
					}
				});
				task.Start ();
				return;
			}

			client.DownloadDataCompleted += async (sender, e) => {
				if (e.Cancelled || e.Error != null)
					return;

				if (e.Result == null || e.Result.Length <= 0)
					return;

				if (scaleWidth > 0 && scaleHeight > 0) {
					o.InSampleSize = CalculateInSampleSize (o, scaleWidth, scaleHeight);
				}

				bmp = await BitmapFactory.DecodeByteArrayAsync (e.Result, 0, e.Result.Length, o);

				if (scaleWidth > 0 && scaleHeight > 0) {
					//bmp = Bitmap.CreateScaledBitmap(bmp, scaleWidth, scaleHeight, true);
				}

				var task = new Task (() => {
					if (fileInfo.Directory != null) {
						fileInfo.Directory.Create ();
					}

					var fileStream = new FileStream (path,
						                 FileMode.OpenOrCreate,
						                 FileAccess.ReadWrite,
						                 FileShare.None);

					bmp.Compress (compressOptions.Format, compressOptions.Quality, fileStream);

					fileStream.Close ();

					_staticRAMCache [urlHash] = bmp;

					if (OnLoad != null) {
						OnLoad (this, bmp);
					}

				});
				task.Start ();
			};

			try {
				client.DownloadDataAsync (uri);
			} catch (WebException ex) {
				Log.Error (TAG, ex.StackTrace);
				var eh = OnException;

				if (eh != null) {
					if (OnException != null) {
						OnException (this, ex);
					}
				}
			}
		}

		public static void LoadImageFromUrl (ImageView iv, string url, BitmapFactory.Options options, int scaleWidth = -1, int scaleHeight = -1, BitmapCompressOptions compressOptions = null)
		{
			var l = new CachedImageLoader (url);

			l.OnLoad += (sender, e) => {
				if (e != null) {
					iv.SetImageBitmap (e);
				}
			};

			l.Load (options, scaleWidth, scaleHeight, compressOptions);
		}
	
	}
}

