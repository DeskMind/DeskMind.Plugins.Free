using System.Windows;
using System.Windows.Controls;

namespace DeskMind.Plugin.PythonRunner.WPF.Helpers
{
    public static class TreeViewSelectedItemBehavior
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(TreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(TreeViewSelectedItemBehavior),
                new PropertyMetadata(false));

        public static object GetSelectedItem(DependencyObject obj) =>
            (object)obj.GetValue(SelectedItemProperty);

        public static void SetSelectedItem(DependencyObject obj, object value) =>
            obj.SetValue(SelectedItemProperty, value);

        private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);
        private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                // hook event only once
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;

                // If VM changed selected item, reflect it in UI by selecting the corresponding TreeViewItem
                if (e.NewValue != null && !ReferenceEquals(e.NewValue, e.OldValue))
                {
                    if (GetIsUpdating(treeView)) return;
                    try
                    {
                        SetIsUpdating(treeView, true);
                        SelectTreeViewItem(treeView, e.NewValue);
                    }
                    finally
                    {
                        SetIsUpdating(treeView, false);
                    }
                }
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView treeView)
            {
                if (GetIsUpdating(treeView)) return;
                try
                {
                    SetIsUpdating(treeView, true);
                    SetSelectedItem(treeView, e.NewValue);
                }
                finally
                {
                    SetIsUpdating(treeView, false);
                }
            }
        }

        private static bool SelectTreeViewItem(ItemsControl parent, object itemToSelect)
        {
            // Try direct container
            var container = parent.ItemContainerGenerator.ContainerFromItem(itemToSelect) as TreeViewItem;
            if (container != null)
            {
                container.IsSelected = true;
                container.BringIntoView();
                return true;
            }

            // Recurse into generated child containers
            foreach (var child in parent.Items)
            {
                var childContainer = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;
                if (childContainer == null)
                    continue; // Not generated yet; will be retried on user expand

                // Ensure expanded so that its children containers can be generated
                var wasExpanded = childContainer.IsExpanded;
                childContainer.IsExpanded = true;
                childContainer.UpdateLayout();

                if (SelectTreeViewItem(childContainer, itemToSelect))
                    return true;

                childContainer.IsExpanded = wasExpanded; // restore
            }

            return false;
        }
    }
}

