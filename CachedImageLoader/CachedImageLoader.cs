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


CONTRIBUTORS:
- Nickolay Simonov <nikotron@rocketmail.com>
- Eugeny Polunin

*/

using System;
using Android.Graphics;
using System.Net;
using Android.App;
using System.IO;
using Android.Widget;
using Android.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace co.littlebyte.Utils
{
	public class BitmapCompressOptions
	{
		public Bitmap.CompressFormat Format = Bitmap.CompressFormat.Png;
		public int Quality = 100;
	}

	public class CachedImageLoader
	{
		private static readonly string TAG = "CachedImageLoader";

		public BitmapFactory.Options BitmapOptions { get; set; }

		private  Queue<Tuple<string,Bitmap, FileInfo, BitmapCompressOptions>> _writeQueue = new Queue<Tuple<string, Bitmap, FileInfo, BitmapCompressOptions>> ();
		private static SemaphoreSlim _loadQueueSemaphore = new SemaphoreSlim (1, 1);

		private static List<ImageView> _loadedImageViews = new List<ImageView> ();
		private readonly string _url;

		private static readonly Dictionary<string, Bitmap> _staticRAMCache = new Dictionary<string, Bitmap> ();

		public static void ClearRAMCache ()
		{
			_staticRAMCache.Clear ();
			_loadedImageViews.Clear ();
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
				Log.Debug (TAG, ex.StackTrace);
			}
			return "";
		}


		public static int CalculateInSampleSize (
			BitmapFactory.Options options, int reqWidth, int reqHeight)
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

		public async Task<Bitmap> LoadInternal (BitmapFactory.Options o = null, int scaleWidth = -1,
		                                        int scaleHeight = -1, BitmapCompressOptions compressOptions = null)
		{
			if (String.IsNullOrEmpty (_url)) {
				return null;
			}

			Bitmap bmp = null;

			if (compressOptions == null) {
				compressOptions = new BitmapCompressOptions ();
			}

			try {
				var uri = new Uri (_url);
				var client = new WebClient ();
				var stringme = String.Format ("{0}{1}{2}", _url, scaleWidth, scaleHeight);
				var urlHash = GetMD5 (new Java.Lang.String (stringme));

				var path = Application.Context.CacheDir.AbsolutePath + "/" + urlHash;
				var fileInfo = new FileInfo (path);

				if (o == null) {
					o = new BitmapFactory.Options () { InPurgeable = true };
				}

				if (_staticRAMCache.ContainsKey (urlHash)) {
					return _staticRAMCache [urlHash];
				}

				if (fileInfo.Exists) {
					using (var file = File.Open (path, FileMode.Open, FileAccess.Read)) {
						byte[] buff = new byte[file.Length];
						await file.ReadAsync (buff, 0, (int)file.Length);
						bmp = await BitmapFactory.DecodeByteArrayAsync (buff, 0, buff.Length, o);
						_staticRAMCache [urlHash] = bmp;

						return bmp;
					}
				} else {
					try {
						Log.Debug ("Loading " + uri.ToString ());
						var response = await client.DownloadDataTaskAsync (uri);

						if (response == null || response.Length <= 0)
							return null;

						if (scaleWidth > 0 && scaleHeight > 0) {
							o.InSampleSize = CalculateInSampleSize (o, scaleWidth, scaleHeight);
						}

						bmp = await BitmapFactory.DecodeByteArrayAsync (response, 0, response.Length, o);
						if (fileInfo.Directory != null) {
							fileInfo.Directory.Create ();
						}

						var fileStream = new FileStream (path,
							                 FileMode.OpenOrCreate,
							                 FileAccess.ReadWrite,
							                 FileShare.ReadWrite);

						await bmp.CompressAsync (compressOptions.Format, compressOptions.Quality, fileStream);

						fileStream.Close ();

						_staticRAMCache [urlHash] = bmp;

						return bmp;

					} catch (WebException ex) {
						Log.Debug (TAG, ex.StackTrace);
						return null;
					}
				}
			} catch (System.UriFormatException ex) {
				Log.Debug (TAG, ex.StackTrace);
				return null;
			}
		}


		public async static void LoadImageFromUrl (ImageView iv, string url, BitmapFactory.Options options = null, int scaleWidth = -1, int scaleHeight = -1, BitmapCompressOptions compressOptions = null)
		{
			await _loadQueueSemaphore.WaitAsync ();

			if (_loadedImageViews.Contains (iv))
				_loadedImageViews.Remove (iv);
			var l = new CachedImageLoader (url);

			var bitmap = await l.LoadInternal (options, scaleWidth, scaleHeight, compressOptions);
			_loadQueueSemaphore.Release ();
			if (bitmap != null && !_loadedImageViews.Contains (iv)) {

				iv.SetImageBitmap (bitmap);
				_loadedImageViews.Add (iv);
			}


		}
	}
}

