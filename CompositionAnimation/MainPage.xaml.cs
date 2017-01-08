using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

namespace CompositionAnimation
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<string> List { get; set; }
        Compositor _compositor;
        SpriteVisual _headerBackShadowVisual;

        public MainPage()
        {
            this.InitializeComponent();
            List = new ObservableCollection<string>();

            this.Loaded += delegate
            {
                _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
                var _scrollViewerPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(FindVisualChild<ScrollViewer>(ItemListView));

                var _scrollExpressionAnimation = _compositor.CreateExpressionAnimation("Clamp(sv.Translation.Y * m, (-200 + 80), 999)");
                _scrollExpressionAnimation.SetReferenceParameter("sv", _scrollViewerPropSet);
                _scrollExpressionAnimation.SetScalarParameter("m", 0.3f);

                var headerBackVisual = ElementCompositionPreview.GetElementVisual(HeaderBackgroundGrid);
                headerBackVisual.StartAnimation("Offset.Y", _scrollExpressionAnimation);

                var _buttonOffsetExpressionAnimation = _compositor.CreateExpressionAnimation("visual.Offset.Y + 200 - 25");
                _buttonOffsetExpressionAnimation.SetReferenceParameter("visual", headerBackVisual);

                var buttonVisual = ElementCompositionPreview.GetElementVisual(AddButtonGrid);
                buttonVisual.StartAnimation("Offset.Y", _buttonOffsetExpressionAnimation);

                var _headerTextOffsetExpressionAnimation = _compositor.CreateExpressionAnimation("Lerp(200 / 2 - 16, 0, Clamp(visual.Offset.Y / (-200 + 80), 0.0, 1.0))");
                _headerTextOffsetExpressionAnimation.SetReferenceParameter("visual", headerBackVisual);

                var textVisual = ElementCompositionPreview.GetElementVisual(HeaderTextGrid);
                textVisual.StartAnimation("Offset.Y", _headerTextOffsetExpressionAnimation);

                _headerBackShadowVisual = _compositor.CreateSpriteVisual();
                var _dropShadow = _compositor.CreateDropShadow();
                _dropShadow.BlurRadius = 12f;
                _dropShadow.Color = Colors.Black;
                _dropShadow.Offset = new Vector3(0, 3f, 0);
                _dropShadow.Opacity = .5f;
                _headerBackShadowVisual.Shadow = _dropShadow;
                _headerBackShadowVisual.Size = new Vector2((float)HeaderBackgroundShadowBorder.ActualWidth, (float)HeaderBackgroundShadowBorder.ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(HeaderBackgroundShadowBorder, _headerBackShadowVisual);

                var _buttonShadowVisual = _compositor.CreateSpriteVisual();
                var _buttonDropShadow = _compositor.CreateDropShadow();
                _buttonDropShadow.BlurRadius = 12f;
                _buttonDropShadow.Color = Colors.Black;
                _buttonDropShadow.Offset = new Vector3(0, 0, 0);
                _buttonDropShadow.Opacity = .5f;
                _buttonDropShadow.Mask = AddButtonShadow.GetAlphaMask();
                _buttonShadowVisual.Shadow = _buttonDropShadow;
                _buttonShadowVisual.Size = new Vector2((float)AddButton.ActualWidth, (float)AddButton.ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(AddButtonShadow, _buttonShadowVisual);
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            for (int i = 0; i < 10; i++) List.Add($"Item {i + 1}");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List.Add($"Item {List.Count + 1}");
        }

        private void HeaderBackgroundShadowBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_headerBackShadowVisual != null)
            {
                _headerBackShadowVisual.Size = new Vector2((float)HeaderBackgroundShadowBorder.ActualWidth, (float)HeaderBackgroundShadowBorder.ActualHeight);
                ElementCompositionPreview.SetElementChildVisual(HeaderBackgroundShadowBorder, _headerBackShadowVisual);
            }
        }

        private void ItemListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (!args.InRecycleQueue)
            {
                args.ItemContainer.Loaded += ItemContainer_Loaded;
            }
        }

        private void ItemContainer_Loaded(object sender, RoutedEventArgs e)
        {
            var itemContainer = sender as ListViewItem;
            var itemsPanel = ItemListView.ItemsPanelRoot as ItemsStackPanel;
            var itemIndex = ItemListView.IndexFromContainer(itemContainer);

            if (itemIndex >= itemsPanel.FirstVisibleIndex && itemIndex <= itemsPanel.LastVisibleIndex)
            {
                if (_compositor == null) _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

                var visual = ElementCompositionPreview.GetElementVisual(itemContainer);

                visual.Scale = new Vector3(1, 1, 1);
                visual.CenterPoint = new Vector3((float)itemContainer.ActualWidth / 2, (float)itemContainer.ActualHeight / 2, 0);
                visual.Size = new Vector2((float)itemContainer.ActualWidth, (float)itemContainer.ActualHeight);
                visual.Offset = new Vector3(0, 0, 0);
                visual.Opacity = 0;

                var fadeAnimation = _compositor.CreateScalarKeyFrameAnimation();
                fadeAnimation.InsertKeyFrame(1f, 1f);
                fadeAnimation.Duration = TimeSpan.FromMilliseconds(600);
                fadeAnimation.DelayTime = TimeSpan.FromMilliseconds(100 * itemIndex);
                fadeAnimation.Target = "Opacity";

                var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
                scaleAnimation.InsertKeyFrame(0, new Vector3(0, 0, 1f));
                scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));
                scaleAnimation.Duration = TimeSpan.FromMilliseconds(500);
                scaleAnimation.DelayTime = TimeSpan.FromMilliseconds(100 * itemIndex);
                scaleAnimation.Target = "Scale";

                var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.InsertKeyFrame(0, new Vector3(0, -200, 0));
                offsetAnimation.InsertKeyFrame(1f, new Vector3(0, 0, 0));
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
                offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(100 * itemIndex);
                offsetAnimation.Target = "Offset";

                var animationGroup = _compositor.CreateAnimationGroup();
                animationGroup.Add(fadeAnimation);
                animationGroup.Add(scaleAnimation);
                animationGroup.Add(offsetAnimation);

                visual.StartAnimationGroup(animationGroup);
            }

            itemContainer.Loaded -= ItemContainer_Loaded;
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }

            return null;
        }
    }
}
