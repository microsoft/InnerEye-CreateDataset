# Introduction

InnerEye-CreateDataset contains tools to convert medical datasets in DICOM-RT format to NIFTI. Datasets converted using
this tool can be consumed directly by [InnerEye-DeepLearning](https://github.com/microsoft/InnerEye-DeepLearning).

Among the core features of this tools are:

- Resampling of the dataset to a common voxel size.
- Renaming of ground truth structures
- Making the structures in a dataset mutually exclusive (this is required by some loss functions in InnerEye-DeepLearning)
- Creating empty structures if they are missing from the dataset
- Discarding subjects that do not have all the required structures
- Augmenting the dataset by combining multiple structures into one, via set operations (intersection, union)
- Removing parts of structures that are lower/higher than other structures in terms of their z coordinate
- Computing statistics of a dataset, to identify outliers and possible annotation errors

## Installing

### Git for Windows

Get the installer from [Git for Windows](https://git-scm.com/download/win)

 The installer will prompt you to "Select Components". Make sure that you tick the boxes for:

- Git LFS (Large File Support)
- Git Credential Manager for Windows

After the installation, open a command prompt or the Git Bash:

- Run `git lfs install` to set up the hooks in git
- Run `git config --global core.autocrlf true` to ensure that line endings are working as expected

Clone the InnerEye-CreateDataset repository on your machine: Run `git lfs clone --recursive https://github.com/microsoft/InnerEye-CreateDataset`

### Visual Studio

You need an installation of Visual Studio 2017. If you have an existing installation, start the Visual Studio Installer, click on "More...", "Modify"

In the "Workloads" section, the following items need to be selected:

- .NET Development
- Desktop development with C++
- Azure Development

In the "Individual Components" section, make sure the following are ticked:

- .NET Core
- Everything with .Net Framework 4.6.2 (and all higher framework versions for good measure)
- VS++ 2015.3 v140 toolset
- Development actitives: F#
- SDK: Windows 10.0.17134.0

Then open the `Source\projects\CreateDataset.sln` solution.

You will see a dialog box suggesting that you upgrade two C++ projects to the latest toolset. Choose **NOT** to upgrade.

Make sure that the required nuget package sources are available for the solution:

- Open Tools->NuGet Package Manager->Package Manager Settings
- Choose NuGet Package Manager->Package Sources
- Add the following sources to the list, if they are not there:

  - nuget: <https://api.nuget.org/v3/index.json>

- Select the above sources, and deselect others

Verify that all projects loaded correctly.

- In the Visual Studio menu, make sure that "Test" / "Test Settings" / "Default Processor Architecture" is set to x64.
- Build the solution. If it fails, build again.

To run tests: After the build, tests should be visible in the Test Explorer.

## Convert a Dicom-RT dataset to NIFTI

To use the tool you will need a DICOM-RT dataset with the ground truth scans and rt-struct files describing
the ground truth segmentations. The folder structure should have the files for each subject in a separate folder. Inside a folder,
the script will search all subdirectories for files as well.

Now, create a parent folder called, for example, `datasets` and place your DICOM-RT dataset folder inside. The folder
structure should resemble the following

```text
* datasets
  * DICOM-RT dataset
    * subject 1
      * DICOM files for subject 1
    * series 2
      * DICOM files for subject 2
    .
    .
    .
```

The simplest form of the command to run is

```cmd
InnerEye.CreateDataset.Runner.exe dataset --datasetRootDirectory=<path to directory holding all datasets> --niftiDatasetDirectory=<name of the folder to write to> --dicomDatasetDirectory=<name of dataset to be converted>
```

- `datasetRootDirectory` is the path to a folder that holds one or more datasets.
- `dicomDatasetDirectory` is the name of the folder, in `datasetRootDirectory`, with the DICOM-RT dataset.
- `niftiDatasetDirectory` is the name of the folder to which the NIFTI dataset should be written.
 This folder will be created in `datasetRootDirectory`
- One common switch is the `geoNorm` switch that performs normalization on the dataset voxel sizes, which takes the sizes in millimeters
for the x, y, and z dimensions. For example `--geoNorm 1;1;2`

A **description of the major commandline options** that control the dataset creation can be found
[here](commandline_args.md).

## Run Analysis on a converted dataset

To analyse a dataset, run

```batch
InnerEye.CreateDataset.Runner.exe analyze  --datasetFolder=<full path to the NIFTI dataset folder to analyse>
```

This will create a folder called `statistics` inside the dataset folder with several csv files containing dataset statistics.
A detailed explanation of the csv files is available [here](/Source/projects/InnerEye.CreateDataset.Common/StatisticsCalculator.cs).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
