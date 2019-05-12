using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.AI.MachineLearning;
using Windows.Media;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace CustomVisionRpi
{
    public sealed partial class MainPage : Page
    {

        private inkshapesModel modelGen;
        private inkshapesInput inkshapesInput = new inkshapesInput();
        private inkshapesOutput inkshapesOutput;

        private Helper                  helper = new Helper();
        RenderTargetBitmap              renderBitmap = new RenderTargetBitmap();

        public MainPage()
        {
            this.InitializeComponent();
            
            // Set supported inking device types.
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | 
                Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(
                new Windows.UI.Input.Inking.InkDrawingAttributes()
                {
                    Color = Windows.UI.Colors.White,
                    Size = new Size(22, 22),
                    IgnorePressure = true,
                    IgnoreTilt = true,
                }
            );
            LoadModelAsync();
        }

        private async Task LoadModelAsync()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/inkshapes.onnx"));
            modelGen = await inkshapesModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
        }

        private async void recognizeButton_Click(object sender, RoutedEventArgs e)
        {

            //Bind model input with contents from InkCanvas
            VideoFrame vf = await helper.GetHandWrittenImage(inkGrid);
            inkshapesInput.data = ImageFeatureValue.CreateFromVideoFrame(vf);

            //Evaluate the model
            inkshapesOutput = await modelGen.EvaluateAsync(inkshapesInput);
            var guessedTag = inkshapesOutput.classLabel.GetAsVectorView().First();
            var loss = inkshapesOutput?.loss?.FirstOrDefault();
            float guessedPercentage = 0f;
            if (loss != null)
            {
                var entry = loss.ToList().OrderByDescending(kv => kv.Value).FirstOrDefault();
                guessedPercentage = entry.Value;
            }

            var text = $"{guessedTag} ({guessedPercentage.ToString("P")})";
            ////Display the results
            numberLabel.Text = text;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            numberLabel.Text = "";
        }
    }
}
