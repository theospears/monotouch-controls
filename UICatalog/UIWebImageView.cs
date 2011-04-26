// --------------
// ESCOZ.COM
// --------------
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;
using System.IO;

namespace escoz
{
	public partial class UIWebImageView : UIImageView
	{
		NSMutableData imageData;
		UIActivityIndicatorView indicatorView;
		string cacheKey;
	
		public UIWebImageView (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public UIWebImageView (NSCoder coder) : base(coder)
		{
			Initialize ();
		}


		void Initialize ()
		{
			indicatorView = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
			indicatorView.HidesWhenStopped = true;
			var width  = (this.Frame.Width-20)/2;
			var height = (this.Frame.Height-20)/2;
			indicatorView.Frame = new RectangleF(width, height,20,20);
			this.AddSubview(indicatorView);
		}
		
		public UIWebImageView(RectangleF frame){
			Initialize();
			
			Frame = frame;
			indicatorView.Frame = new RectangleF (
                		frame.Size.Width/2,
                		frame.Size.Height/2,
                		indicatorView.Frame.Size.Width,
                		indicatorView.Frame.Size.Height);

		}
		
		public void LoadImage(string cacheKey, string url){
			this.cacheKey = cacheKey;
			
			// Check in cache
			string cacheFile = GetCachePath(cacheKey);
			if(File.Exists(cacheFile))
			{
				Image = UIImage.FromFile(cacheFile);
			}
			else
			{
				// Otherwise download
				indicatorView.StartAnimating();
				NSUrlRequest request = new NSUrlRequest(new NSUrl(url));
				
				new NSUrlConnection(request, new ConnectionDelegate(SetImageData), true);
			}
		}
		
		private string GetCachePath(string cacheKey)
		{
			string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),"ImageCache");
			return Path.Combine(cacheDir, cacheKey);
			
		}
		
		private void SetImageData(NSData data)
		{
			indicatorView.StopAnimating();
			UIImage downloadedImage = UIImage.LoadFromData(data);
			Image = downloadedImage;
			
			// Save to cache
			string fileCache = GetCachePath(cacheKey);
			NSError error;
			Directory.CreateDirectory(Path.GetDirectoryName(fileCache));
			data.Save(fileCache, false, out error);
		}
		
		public override UIColor BackgroundColor {
			get {
				return base.BackgroundColor;
			}
			set {
				base.BackgroundColor = value;
				float hue, saturation, brightness, alpha;
				value.GetHSBA(out hue, out saturation, out brightness, out alpha);
				
				if(brightness < 0.5)
				{
					indicatorView.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.White;
				}
				else
				{
					indicatorView.ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.Gray;
				}
			}
		}
		
		class ConnectionDelegate : NSUrlConnectionDelegate {
			
			NSMutableData imageData = null;
			Action<NSData> _imageSetter;
			
			public ConnectionDelegate(Action<NSData> imageSetter){
				_imageSetter = imageSetter;
			}
			
			public override void ReceivedData (NSUrlConnection connection, NSData data)
			{
				if (imageData==null)
					imageData = new NSMutableData();
				
				imageData.AppendData(data);	
			}
			
			public override void FinishedLoading (NSUrlConnection connection)
			{
				_imageSetter(imageData);
				imageData.Dispose();
				imageData = null;
			}
		}
	}
}
