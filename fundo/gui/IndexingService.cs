using fundo.core.Search.Index;
using fundo.tool;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fundo.gui
{
    internal class IndexingService
    {
        private CancellationTokenSource? cancellationTokenSource;
        private readonly XamlRoot xamlRoot;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly List<Drive> drivesToIndex;

        public IndexingService(XamlRoot xamlRoot, List<Drive> drivesToIndex)
        {
            this.xamlRoot = xamlRoot ?? throw new ArgumentNullException(nameof(xamlRoot));
            this.dispatcherQueue = xamlRoot.Content.DispatcherQueue;
            this.drivesToIndex = drivesToIndex.Where(d => d.IsSelected).ToList();
        }

        public async Task StartIndexingAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();

            await ShowProgressDialogAsync(cancellationTokenSource.Token);
        }

        private async Task ShowProgressDialogAsync(CancellationToken cancellationToken)
        {
            TextBlock statusText = new TextBlock
            {
                Text = "Preparing indexing...",
                TextWrapping = TextWrapping.Wrap,
                Width = 400,
                Margin = new Thickness(0, 10, 0, 0)
            };

            ProgressRing progressRing = new ProgressRing
            {
                IsActive = true,
                IsIndeterminate = false,
                Width = 50,
                Height = 50,
                Maximum = drivesToIndex.Count,
                Value = 0
            };

            StackPanel content = new StackPanel
            {
                Width = 450,
                Children = { progressRing, statusText }
            };

            ContentDialog progressDialog = new ContentDialog
            {
                Title = "Indexing in Progress",
                Content = content,
                PrimaryButtonText = "Cancel",
                XamlRoot = xamlRoot
            };

            progressDialog.PrimaryButtonClick += (s, args) =>
            {
                cancellationTokenSource?.Cancel();
            };

            Task indexingTask = Task.Run(async () =>
            {
                try
                {
                    await PerformIndexingAsync(cancellationToken, 
                        (status) =>
                        {
                            dispatcherQueue.TryEnqueue(() =>
                            {
                                statusText.Text = status;
                            });
                        },
                        (progress) =>
                        {
                            dispatcherQueue.TryEnqueue(() =>
                            {
                                progressRing.Value = progress;
                            });
                        });

                    dispatcherQueue.TryEnqueue(() =>
                    {
                        progressDialog.Hide();
                    });
                }
                catch (OperationCanceledException)
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        statusText.Text = "Indexing was cancelled.";
                        progressRing.IsActive = false;
                        progressDialog.PrimaryButtonText = "Close";
                    });
                }
                catch (Exception ex)
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        statusText.Text = $"Error: {ex.Message}";
                        progressRing.IsActive = false;
                        progressDialog.PrimaryButtonText = "Close";
                    });
                }
            });

            await progressDialog.ShowAsync();

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        private async Task PerformIndexingAsync(CancellationToken cancellationToken,
            Action<string> updateStatus,
            Action<int> updateProgress)
        {
            if (drivesToIndex.Count == 0)
            {
                updateStatus("No drives selected for indexing.");
                await Task.Delay(2000, cancellationToken);
                return;
            }

            int n = 0;
            foreach (var drive in drivesToIndex)
            {
                cancellationToken.ThrowIfCancellationRequested();

                updateStatus($"Indexing drive {drive.DriveLetter}");

                await Task.Delay(1000, cancellationToken);

                n++;
                updateProgress(n);
            }

            updateStatus("Indexing completed!");
            await Task.Delay(1500, cancellationToken);
        }
    }
}
