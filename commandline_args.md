# Commandline options for InnerEye-CreateDataset

The list below contains the main commandline switches for InnerEye-CreateDataset. For a full list of options, compile
the `InnerEye.CreateDataset.Runner` project and run `InnerEye.CreateDataset.Runner.exe dataset --help`.

The options are defined in [CommandlineCreateDataset.cs](https://github.com/microsoft/InnerEye-CreateDataset/blob/main/Source/projects/InnerEye.CreateDataset.Core/Commandline/CommandlineCreateDataset.cs).

## `--rename`: Renaming and modifying ground truth structures

The argument value is a mapping of structure names in the Dicom dataset to structure names in the nifti dataset.
All structure names will be converted to lower case.

A list of structure name mappings that should be applied to the dataset before doing any other operations on the dataset.
Example: 'A,B_something:C' means that all structures called A or B_something in the dicom dataset should be called C
in the nifti dataset.

If there are multiple structures that would map to the same name, the first one is preferred. For example, if the series
contains structures "Adam" and "Eve", and renaming is run with the mapping "adam,eve:charly", the input structure
"Adam" would be renamed to "charly". "Eve" will be left untouched, apart from lowercasing to "eve".

If the right hand side of the expression (after the colon) is prefixed by "+", then an augmentation rather than a
renaming takes place: if "C" already exists, the voxels in each left-hand-side structure are added to C rather than
replacing it (and the left-hand-side structures are kept).

**Set operations**: On the left hand side, structures may be specified as "A.op.B", where "op" is one of "gt,ge,lt,le,intersection,union".
In this case, the source for the renaming (or augmentation) is computed from A and B if they both exist. The comparison
operators refer to the vertical (z) dimension, so "A.gt.B" means "all voxels in A whose z value is greater than that of
any voxel in B".

In the case of a renaming (expression ends with ":C"), if one left-hand-side element is successful, later ones are
not tried; in the case of an augmentation, all elements are tried, and the augmentations are cumulative.

In general, you will want to have all augmentations listed after all renamings, so that you know what the structure
names will be by the time you try the augmentations.

Example: `--rename "whole brain,whole_brain:brain;subtract:nec_cav"` will rename structures called "whole brain" or "whole_brain" to "brain", and "subtract" to "nec_cav". Note that structure names here contain spaces, so you need to quote the whole expression.

Example: `--rename whole_tumour.intersection.nec_cav:rim` will subtract voxels in `nec_cav` from those in `whole_tumour`, and rename the result to `rim`.

## `--geoNorm`: Controls the resampling of the dataset

The spacings to use for geometric normalization, in millimeters.
3 values must be provided, semicolon separated, in the order X; Y; Z.
Using a spacing of 0 in any dimension means to not change this specific dimension.

Example: `--geoNorm 1;1;0` will normalize the spacing to 1mm in the X and Y dimensions, and not change the third dimension.

## `--groundTruthDescendingPriority`: Controls the order of structures when making the dataset mutually exclusive

The priority mapping for structures, to ensure they are mutually exclusive. Only structures with names in this set will be uded in the
resulting dataset. Special case: if names are prefixed by "+", structures with those names will be included, but mutual usion will not be
enforced between those structures and any others.

Example: `--groundTruthDescendingPriority seminalvesicles;prostate;bladder;femur_l;femur_r;rectum;external`

## `--createIfMissing`: Generate empty structures if needed

The names of the structures, semicolon separated, that should be created if they are missing from the DICOM dataset.
This is done after structure renaming.

Example: `--createIfMissing nec_cav;edema` will create empty Nifti masks names `nec_cav` and `edema` if no such structures
exist in the dataset yet.

## `--discardInvalidSubjects`

If this switch is provided, invalid subjects will be discarded, and the dataset
will be created without them. If not provided, the program will exit when encountering the first invalid subject.
If you set this switch, you should check the output of the job for information about discarded subjects.

## `--allowNameClashes`

If this switch is provided (True), structure renaming causes a choice to be made between named structures when several options exist, er than causing
renaming to fail. Specifically, for a name mapping "oldName1,oldName2,...:newName":

- If AllowNameClashes is not set and structures exist for more than one of the names (old and/or new), renaming s.
- Otherwise, if a structure named newName exists, no renaming is done.
- Otherwise, if a structure named oldNameK exists and no structures with names oldName1, ..., oldName[K-1] t, then it is
renamed to newName.

Thus in summary, if names clash, an existing "newName" structure is preferred (we assume the oldNames are -preferred variants
of the canonical newName), otherwise earlier "oldName"s in the list have priority over later ones.

## `--requireAllGroundTruthStructures`

If this switch is provided alongside `--groundTruthDescendingPriority`, then all structures that are given in
`--groundTruthDescendingPriority` must be present. If any are not present,
then either that volume is discarded (if `--discardInvalidSubjects` is set) or an exception is raised.

## `--dropNamesContaining <names>`

Drop any structures with names containing the specified substring before name mappings are applied. This will usually have the
effect (depending on other switches) of images containing such structures being discarded.

This is helpful to remove auto-generated structures that would otherwise be included in the dataset, and bloat its size.

Example: `--dropNamesContaining planning` will drop all structures containing the string `planning`.

## `--renameIndex` and `--priorityIndex`: Support for built-in complex renaming

The dataset creation tool has been used to create datasets for segmenting Head and Neck tumors, where we saw a huge
variability in structure names. This made it almost impossible to specify everything on the command line.
The `--renameIndex` switch allows to specify a built-in set of renaming rules.

These rules are specified in [CommandlineCreateDatasetRecipes.cs](https://github.com/microsoft/InnerEye-CreateDataset/blob/main/Source/projects/InnerEye.CreateDataset.Core/Commandline/CommandlineCreateDatasetRecipes.cs), in the variable `PrespecifiedNameMappings`.

Example: `--renameIndex 0` will apply the first set of renaming rules.

Similarly, the `--priorityIndex` switch allows to specify a built-in set of structure priorities, that are defined in
the variable `PrespecifiedGroundTruthDescendingPriorities` in [CommandlineCreateDatasetRecipes.cs](https://github.com/microsoft/InnerEye-CreateDataset/blob/main/Source/projects/InnerEye.CreateDataset.Core/Commandline/CommandlineCreateDatasetRecipes.cs)

Example: `--priorityIndex 0` will apply the first set of structure priorities.

You can add your own renaming rules and structure priorities to the code, recompile the tool, and then use them with the `--renameIndex` and `--priorityIndex` switches.
