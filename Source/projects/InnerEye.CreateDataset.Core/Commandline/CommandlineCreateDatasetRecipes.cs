///  ------------------------------------------------------------------------------------------
///  Copyright (c) Microsoft Corporation. All rights reserved.
///  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
///  ------------------------------------------------------------------------------------------

﻿namespace InnerEye.CreateDataset.Core
{
    class CommandlineCreateDatasetRecipes
    {

        /// <summary>
        /// Array of arrays of (parsed) prespecified name mappings. The value of the switch "--renameIndex"
        /// should be a valid index of this array, and the relevant (sub)array is then used instead of
        /// the result of parsing the value of "--rename".
        /// </summary>
        public static readonly NameMapping[][] PrespecifiedNameMappings = new[]
        {
            // --renameIndex=0
            new[]
            {
                // This one has to come before its uses for mpc_muscle and spc_muscle
                new NameMapping("almost_full_pc", new[] {
                    "full_pc.lt.toppc_split",
                    "full_pcag.lt.toppc_split",
                }),
                new NameMapping("brain", new[] {
                    "brainag",
                    "brain",
                    "brain2",
                    "brian",
                    "whole brain",
                    "whole brain1",
                    "wholebrain"
                }),
                new NameMapping("brainstem", new[] {
                    "brainstemag",
                    "brainstem ag",
                    "brainstem_ag",
                    "brainstem",
                    "brain stem",
                    "brainstem_new",
                    "avoid b stem",
                    "bs opt",
                    "bsopt",
                }),
                new NameMapping("cochlea_l", new[] {
                    "cochlea_lag",
                    "left cochlea",
                    "cochlea l",
                    "cochlea_l",
                    "l cochlea",
                    "cochlea lt",
                    "lcochlea",
                    "lt cochlea",
                    "lt cochlea_new"
                }),
                new NameMapping("cochlea_r", new[] {
                    "cochlea_rag",
                    "right cochlea",
                    "cochlea r",
                    "r cochlea",
                    "cochlea rt",
                    "cochlea_r",
                    "right coclea",
                    "rt cochlea",
                    "rt cochlea_new",
                }),
                new NameMapping("external", new[] {
                    "external",
                    "skin",
                    "skinshell",
                    "surfaceskin"
                }),
                new NameMapping("globe_l", new[] {
                    "globe_lag",
                    "globe_l",
                    "left globe",
                    "l eye",
                    "l_eye",
                    "eye left",
                    "eye_l",
                    "l globe",
                    "left eye",
                    "left orbit",
                    "lt globe"
                }),
                new NameMapping("globe_r", new[] {
                    "globe_rag",
                    "globe-rag",
                    "right globe",
                    "r eye",
                    "r_eye",
                    "globe_r",
                    "r globe",
                    "eye right",
                    "eye_r",
                    "right eye",
                    "rt eye",
                    "right orbit",
                    "rt globe"
                }),
                new NameMapping("lacrimal_gland_l", new[]
                {
                    "lacrimal_gland_lag",
                    "lacrimalgland_lag",
                    "lacrimal_gland_l",
                    "lacrimalgland_l",
                    "l_lacrimal gland",
                    "lacrimal l",
                    "lacrimal gd_l",
                    "l lacrimal gland",
                }),
                new NameMapping("lacrimal_gland_r", new[]
                {
                    "lacrimal_gland_rag",
                    "lacrimalgland_rag",
                    "lacrimal_gland_r",
                    "lacrimalgland_r",
                    "r_lacrimal gland",
                    "lacrimal r",
                    "lacrimal gd_r",
                    "r lacrimal gland",
                }),
                new NameMapping("lens_l", new[]
                {
                    "lens_lag",
                    "lens_l",
                    "left lens",
                    "left lens_resected",
                    "l_lens",
                    "l lens",
                    "l lense",
                    "leftlens",
                    "lens l",
                    "lens left",
                    "lt lens",
                    "ltlens",
                }),
                new NameMapping("lens_r", new[]
                {
                    "lens_rag",
                    "lens_r",
                    "right lens",
                    "right lens_resected",
                    "r_lens",
                    "lens r",
                    "lens right",
                    "r lens",
                    "r lense",
                    "right  lens",
                    "rightlens",
                    "rigth lens",
                    "rlens",
                    "rt lens",
                    "rtlens",
                }),
                new NameMapping("lung_l", new[] {
                    "lung_lag",
                }),
                new NameMapping("lung_r", new[] {
                    "lung_rag",
                }),
                new NameMapping("mandible", new[] {
                    "mandibleag",
                    "mandible_ag",
                    "mandible",
                    "manbible",
                    "manidble",
                    "manidible",
                    "jaw",
                }),
                new NameMapping("mpc_muscle", new[] {
                    "mpc",
                    "mpc muscle",
                    "mpc muscles",
                    "pcm_middle",
                    "middle constrictor",
                    "mpc_muscle_ag",
                    "mpc_muscleag",
                    "almost_full_pc.lt.midpc_split",
                }),
                new NameMapping("optic_chiasm", new[] {
                    "optic_chiasmag",
                    "optic chiasmag",
                    "opticchiasmag",
                    "chiasm_ag",
                    "optic_chiasm_ag",
                    "optic chiasm ag",
                    "optic chiasm",
                    "chiasm",
                    "oc",
                }),
                new NameMapping("optic_nerve_l", new[] {
                    "l optic nerve ag",
                    "optic nerve_l",
                    "opticnerve_l",
                    "opticnerve_lag",
                    "l_optic nerve",
                    "optic nerve l",
                    "on  l",
                    "on_l",
                    "l on",
                    "l optic nerve",
                    "left on",
                    "left optic nerve",
                    "left optic neve",
                    "lt optic n",
                    "lt optic nerve",
                    "on left",
                    "optic n_l",
                    "optic nerve left",
                }),
                new NameMapping("optic_nerve_r", new[] {
                    "r optic nerve ag",
                    "right optic nerve",
                    "optic nerve_r",
                    "opticnerve_r",
                    "opticnerve_rag",
                    "r_optic nerve",
                    "optic nerve r",
                    "on r",
                    "on_r",
                    "r on",
                    "on right",
                    "optic n_r",
                    "optic nerve right",
                    "optical nerve right",
                    "r optic nerve",
                    "right on",
                    "rightopticnerve",
                    "ron",
                    "rt optic n",
                    "rt optic nerve",
                }),
                new NameMapping("parotid_l", new[] {
                    "parotid_lag",
                    "parotid_l_ag",
                    "left parotid",
                    "parotid_l",
                    "parotid l",
                    "parotid_cl",
                    "parotid_il",
                    "parotid_l_new",
                    "parotid_lt",
                    "parotidl",
                    "l parotid",
                    "l_parotid",
                    "lt parotid",
                    "left parotid_resected",
                }),
                new NameMapping("parotid_r", new[] {
                    "parotid_rag",
                    "parotid _rag",
                    "parotid_r_ag",
                    "parotid_r",
                    "right parotid",
                    "parotid_r_new",
                    "parotid_rt",
                    "parotidr opt",
                    "parotidr",
                    "rt parotid",
                    "parotid _r",
                    "parotid r",
                    "r parotid",
                    "r_parotid",
                    "rt parotid",
                    "parotid_rag_resected",
                    "right parotid_resected",
                }),
                new NameMapping("pharyngeal_constrictor", new[] {
                    "pharyngeal_constrictorag",
                    "pharyngeal constrictor",
                }),
                new NameMapping("pituitary_gland", new[] {
                    "pituitary_glandag",
                    "pituitary glandag",
                    "pituitaryag",
                    "pituitary gland",
                    "pituitary",
                    "pituitary_ag",
                    "pituitary ag",
                }),
                new NameMapping("smg_l", new[] {
                    "smg_lag",
                    "smg_l_ag",
                    "smg_l",
                    "l_smg_resected",
                    "smg_lag_resected",
                    "smg_l_resected",
                    "left smg_resected",
                    "left smg resected",
                    "left smg",
                    "lsmg",
                    "left submandibular",
                    "lt subandibular gland",
                    "lt submandibular gland",
                    "lt submandibular opt",
                    "lt submandibular",
                    "lt submandibular_new",
                    "l smg",
                    "l submandibular gland",
                    "l submandibular",
                    "l_smg",
                    "smg l",
                    "smgl",
                    "smg-l",
                    "sub mand_l",
                    "submand_l",
                    "submandib_l",
                    "submandibular l",
                    "submandibular_l",
                    "submandibular gland l",
                    "sbmg l",
                    "left smg",
                }),
                new NameMapping("smg_r", new[] {
                    "smg_rag",
                    "smg_r_ag",
                    "smg_r",
                    "right smg_resected",
                    "right smg resected",
                    "smg_rag_resected",
                    "smg_r_resected",
                    "right smg",
                    "r_smg",
                    "r smg",
                    "right submandibular",
                    "r submandibular gland",
                    "r submandibular",
                    "smg r",
                    "smgr",
                    "rsmg",
                    "rt submandibular gland",
                    "rt submandibular",
                    "submand_r",
                    "submandib_r",
                    "submandibular gland r",
                    "submandibular r",
                    "submandibular_r",
                    "submandibular gland r",
                    "right smg",
                }),
                new NameMapping("spc_muscle", new[] {
                    "spc",
                    "spc_muscle",
                    "spc_muscle_",
                    "spc muscle",
                    "spc muscles",
                    "pcm_superior",
                    "superior constrictor",
                    "spc_muscle_ag",
                    "spc_muscleag",
                    "almost_full_pc.ge.midpc_split",
                }),
                new NameMapping("spinalcanal", new[] {
                    "spinal canal",
                }),
                new NameMapping("spinal_cord", new[] {
                    "spinalcord_ag",
                    "spinal_cord_ag",
                    "spinal_cordag",
                    "spinal cord_ag",
                    "spinal cord",
                    "spinal_cord",
                    "spinal cord_new",
                    "spinal cord_rj",
                    "spinalcord",
                    "cord",
                }),
                // Augmentations from here.
                // Any brainstem voxels below, or at, the top_of_peg structure are converted to spinal_cord.
                new NameMapping("spinal_cord", new []
                {
                    "brainstem.le.top_of_peg",
                },
                    isAugmentation: true),
                // Any spinal_cord voxels above the top_of_peg structure are converted to brainstem.
                new NameMapping("brainstem", new []
                {
                    "spinal_cord.gt.top_of_peg",
                },
                    isAugmentation: true),
                // Any spinal cord voxels below the manubrium sternum are converted to spinal_cord_excess (which is discarded).
                new NameMapping("spinal_cord_excess", new []
                {
                    "spinal_cord.lt.ms",
                },
                    isAugmentation: true),
            }
        };

        /// <summary>
        /// Array of arrays of prespecified ground-truth structure names. The value of the switch "--groundTruthDescendingPriorityIndex"
        /// should be a valid index of this array, and the relevant (sub)array is then used instead of the value of "--groundTruthDescendingPriority".
        /// </summary>
        public static readonly string[][] PrespecifiedGroundTruthDescendingPriorities = new[]
        {
            // --priorityIndex=0, for external only
            new[] { "external" },
            // --priorityIndex=1, for release 1.
            new[]
            {
                "globe_l", "globe_r",
                "smg_l", "smg_r", "spc_muscle", "mpc_muscle",
                "spinal_cord", "parotid_l", "parotid_r", "brainstem", "mandible", "external"
            },
            // --priorityIndex=2, for probable release 2: set 1 plus lenses, cochleas, optics, pituitary
            new[]
            {
                "lens_l", "lens_r", "cochlea_l", "cochlea_r", "globe_l", "globe_r",
                "optic_chiasm", "optic_nerve_l", "optic_nerve_r", "pituitary_gland",
                "smg_l", "smg_r", "spc_muscle", "mpc_muscle",
                "spinal_cord", "parotid_l", "parotid_r", "brainstem", "mandible", "external"
            },
            // --priorityIndex=3, for ambitious release 2: set 2 plus lacrimal glands
            new[]
            {
                "lacrimal_gland_l", "lacrimal_gland_r",
                "lens_l", "lens_r", "cochlea_l", "cochlea_r", "globe_l", "globe_r",
                "optic_chiasm", "optic_nerve_l", "optic_nerve_r", "pituitary_gland",
                "smg_l", "smg_r", "spc_muscle", "mpc_muscle",
                "spinal_cord", "parotid_l", "parotid_r", "brainstem", "mandible", "external"
            },
            // --priorityIndex=4, all reasonable structures: set 3 plus pharyngeal_constrictor, spinal canal, lungs, brain
            // (mainly for determining what images contain what structures).
            new[]
            {
                "lacrimal_gland_l", "lacrimal_gland_r",
                "lens_l", "lens_r", "cochlea_l", "cochlea_r", "globe_l", "globe_r",
                "optic_chiasm", "optic_nerve_l", "optic_nerve_r", "pituitary_gland",
                "smg_l", "smg_r", "spc_muscle", "mpc_muscle", "pharyngeal_constrictor",
                "spinal_cord", "spinal_canal", "parotid_l", "parotid_r", "brainstem", "brain",
                "lung_l", "lung_r", "mandible", "external"
            },
            // --priorityIndex=5, for comparison with MLV1.
            new []
            {
                "smg_l", "smg_r", "spinal_cord", "parotid_l", "parotid_r", "external"
            },
            // --priorityIndex=6, for comparison with MLV2.
            new []
            {
                "smg_l", "smg_r", "globe_l", "globe_r", "spinal_cord", "parotid_l", "parotid_r", "brainstem", "mandible", "external"
            },
        };
    }
}
