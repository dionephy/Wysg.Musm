using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.Services;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Test window for API connectivity
    /// Use this during development to verify API integration
    /// </summary>
    public partial class ApiTestWindow : Window
    {
        private readonly ApiTestService _apiTestService;

        public ApiTestWindow()
        {
            InitializeComponent();

            // Get ApiTestService from DI container
            var app = (App)Application.Current;
            _apiTestService = app.Services.GetRequiredService<ApiTestService>();

            Log("API Test Window loaded");
            Log("Make sure API is running on http://localhost:5205");
            Log("Click 'Quick Test' to start\n");
        }

        private async void QuickTestButton_Click(object sender, RoutedEventArgs e)
        {
            QuickTestButton.IsEnabled = false;
            FullTestButton.IsEnabled = false;

            try
            {
                Log("???????????????????????????????????????");
                Log("Starting Quick Test...");
                Log("???????????????????????????????????????\n");

                SetStatus("Testing...", Colors.Orange);

                var success = await _apiTestService.QuickTestAsync();

                if (success)
                {
                    Log("\n? Quick test passed!");
                    SetStatus("? Quick test passed", Colors.Green);
                }
                else
                {
                    Log("\n? Quick test failed");
                    SetStatus("? Quick test failed", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                Log($"\n? Error: {ex.Message}");
                SetStatus($"? Error: {ex.Message}", Colors.Red);
            }
            finally
            {
                QuickTestButton.IsEnabled = true;
                FullTestButton.IsEnabled = true;
            }
        }

        private async void FullTestButton_Click(object sender, RoutedEventArgs e)
        {
            QuickTestButton.IsEnabled = false;
            FullTestButton.IsEnabled = false;

            try
            {
                Log("???????????????????????????????????????");
                Log("Starting Full Test Suite...");
                Log("???????????????????????????????????????\n");

                SetStatus("Running full tests...", Colors.Orange);

                var success = await _apiTestService.RunAllTestsAsync();

                if (success)
                {
                    Log("\n? All tests passed!");
                    SetStatus("? All tests passed", Colors.Green);
                    
                    MessageBox.Show(
                        "All API tests passed successfully!\n\n" +
                        "Your API integration is working correctly.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    Log("\n? Some tests failed");
                    SetStatus("? Some tests failed", Colors.Red);
                    
                    MessageBox.Show(
                        "Some tests failed.\n\n" +
                        "Check the log output above for details.\n" +
                        "Make sure:\n" +
                        "1. API is running (http://localhost:5205)\n" +
                        "2. You're logged in\n" +
                        "3. Firebase token is set",
                        "Tests Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Log($"\n? Error: {ex.Message}");
                SetStatus($"? Error: {ex.Message}", Colors.Red);
            }
            finally
            {
                QuickTestButton.IsEnabled = true;
                FullTestButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LogText.Text = string.Empty;
            SetStatus("Log cleared", Colors.Gray);
            Log("Log cleared. Ready for new tests.\n");
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogText.Text += message + "\n";
                
                // Auto-scroll to bottom
                var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(this);
                scrollViewer?.ScrollToEnd();
            });

            // Also write to Debug output
            Debug.WriteLine($"[ApiTestWindow] {message}");
        }

        private void SetStatus(string message, Color color)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                StatusText.Foreground = new SolidColorBrush(color);
            });
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
