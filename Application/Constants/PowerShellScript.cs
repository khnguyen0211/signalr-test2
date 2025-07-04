namespace Application.Constants
{
    public class PowerShellScript
    {
        public const string GetSystemInformation = @"
                                                    $logicalDisks   = Get-CimInstance Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3};
                                                    $CPUType        = (Get-CimInstance Win32_Processor).Name;
                                                    if($CPUType -like '*Intel*'){$workStation = 'Intel'} elseif($CPUType -like '*AMD*'){$workStation = 'AMD'} else{$workStation = 'Other'}
                                                    [PSCustomObject]@{
                                                    OperatingSystem = (Get-CimInstance Win32_OperatingSystem).Caption
                                                    WorkstationType = $workStation
                                                    CPUType         = $CPUType
                                                    Ram             = '{0} GB' -f ([Math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 0))
                                                    GPUType         = (Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name) -join '; '
                                                    Storage         = '{0} GB' -f ([Math]::Floor(($logicalDisks | Measure-Object -Property Size -Sum).Sum / 1GB))
                                                    } | ConvertTo-Json";
    }
}
