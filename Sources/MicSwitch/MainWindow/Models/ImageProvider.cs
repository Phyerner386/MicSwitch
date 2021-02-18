using System;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;
using log4net;
using MicSwitch.Modularity;
using MicSwitch.Services;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using ReactiveUI;
using Unity;

namespace MicSwitch.MainWindow.Models
{
    internal sealed class ImageProvider : DisposableReactiveObject, IImageProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ImageProvider));

        private readonly IMicrophoneControllerEx microphoneController;
        private readonly BitmapImage defaultMicrophoneImage = new BitmapImage(new Uri("pack://application:,,,/Resources/microphoneEnabled.ico", UriKind.RelativeOrAbsolute));
        private readonly BitmapImage defaultMutedMicrophoneImage = new BitmapImage(new Uri("pack://application:,,,/Resources/microphoneDisabled.ico", UriKind.RelativeOrAbsolute));

        private ImageSource streamingMicrophoneImage;
        private ImageSource mutedMicrophoneImage;
        private ImageSource microphoneImage;

        public ImageProvider(
            [NotNull] IMicrophoneControllerEx microphoneController,
            [NotNull] IConfigProvider<MicSwitchConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.microphoneController = microphoneController;
            
            configProvider.ListenTo(x => x.MicrophoneLineId)
                .ObserveOn(uiScheduler)
                .SubscribeSafe(lineId => microphoneController.LineId = lineId, Log.HandleUiException)
                .AddTo(Anchors);

            Observable.Merge(
                    microphoneController.WhenAnyValue(x => x.Mute).ToUnit(),
                    this.WhenAnyValue(x => x.StreamingMicrophoneImage).ToUnit(),
                    this.WhenAnyValue(x => x.MutedMicrophoneImage).ToUnit()
                )
                .ObserveOn(uiScheduler)
                .Select(x => microphoneController.Mute ?? false ? mutedMicrophoneImage : streamingMicrophoneImage)
                .SubscribeSafe(x => MicrophoneImage = x, Log.HandleUiException)
                .AddTo(Anchors);

            configProvider.ListenTo(x => x.MicrophoneIcon)
                .ObserveOn(uiScheduler)
                .SelectSafeOrDefault(x => x.ToBitmapImage())
                .SubscribeSafe(x => StreamingMicrophoneImage = x ?? defaultMicrophoneImage, Log.HandleUiException)
                .AddTo(Anchors);
            
            configProvider.ListenTo(x => x.MutedMicrophoneIcon)
                .ObserveOn(uiScheduler)
                .SelectSafeOrDefault(x => x.ToBitmapImage())
                .SubscribeSafe(x => MutedMicrophoneImage = x ?? defaultMutedMicrophoneImage, Log.HandleUiException)
                .AddTo(Anchors);
        }

        public ImageSource MicrophoneImage
        {
            get => microphoneImage;
            private set => RaiseAndSetIfChanged(ref microphoneImage, value);
        }

        public ImageSource StreamingMicrophoneImage
        {
            get => streamingMicrophoneImage;
            private set => RaiseAndSetIfChanged(ref streamingMicrophoneImage, value);
        }

        public ImageSource MutedMicrophoneImage
        {
            get => mutedMicrophoneImage;
            private set => RaiseAndSetIfChanged(ref mutedMicrophoneImage, value);
        }
    }
}