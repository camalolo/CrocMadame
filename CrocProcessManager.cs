using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrocMadame;

public class CrocProcessManager
{
    private Process? _crocProcess;
    private readonly object _outputLock = new object();

    public async Task<int> StartProcess(string arguments, Action<string> appendOutput, Action<double> updateProgress)
    {
        try
        {
            _crocProcess = new Process();
            _crocProcess.StartInfo.FileName = "croc";
            _crocProcess.StartInfo.Arguments = arguments;
            _crocProcess.StartInfo.UseShellExecute = false;
            _crocProcess.StartInfo.RedirectStandardOutput = true;
            _crocProcess.StartInfo.RedirectStandardError = true;
            _crocProcess.StartInfo.CreateNoWindow = true;

            _crocProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (TryParseProgress(e.Data, out double progress))
                    {
                        updateProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        appendOutput(e.Data);
                    }
                }
            };

            _crocProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (TryParseProgress(e.Data, out double progress))
                    {
                        updateProgress(progress);
                    }
                    else if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        appendOutput(e.Data);
                    }
                }
            };

            _crocProcess.Start();
            _crocProcess.BeginOutputReadLine();
            _crocProcess.BeginErrorReadLine();

            await Task.Run(() => _crocProcess.WaitForExit());

            return _crocProcess.ExitCode;
        }
        catch (Exception ex)
        {
            appendOutput($"Error: {ex.Message}");
            return -1;
        }
        finally
        {
            _crocProcess = null;
        }
    }

    public void KillProcess()
    {
        if (_crocProcess != null && !_crocProcess.HasExited)
        {
            _crocProcess.Kill();
            _crocProcess = null;
        }
    }

    private bool TryParseProgress(string line, out double progress)
    {
        progress = 0;
        var match = Regex.Match(line, @"(\d+)%\s*\|\s*.*\|\s*\(");
        if (match.Success && double.TryParse(match.Groups[1].Value, out progress))
        {
            return true;
        }
        return false;
    }
}