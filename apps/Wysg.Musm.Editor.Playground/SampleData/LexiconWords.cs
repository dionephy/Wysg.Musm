namespace Wysg.Musm.Editor.Playground.SampleData;

public static class LexiconWords
{
    // Add anything you want to test (English + radiology terms).
    public static readonly string[] Words = new[]
    {
        // General (helpful for “th” prefix)
        "the", "there", "therefore", "therapy", "theta", "thin", "thick", "thing",
        "think", "third", "thorough", "though", "thought", "through", "throughout",

        // Radiology / neuro
        "thalamus", "thalamic hemorrhage", "thalamic infarct",
        "thorax", "thoracic spine", "thoracic aorta", "thoracic outlet",
        "thyroid", "thyroid nodule", "thoracostomy", "thrombosis",
        "thrombus", "thromboembolism", "thrombus in transit",
        "thalamocortical", "thalamostriate vein",
        "thin-section CT", "thin-slice reconstruction",
        "three-dimensional MRA", "three-vessel runoff",
        "thickened wall", "thalamic nuclei", "thoracolumbar junction",

        // Extra common clinical phrasing
        "no acute intracranial hemorrhage",
        "no flow-limiting stenosis",
        "findings are compatible with acute infarction",
        "there is no evidence of aneurysm",
        "there is no significant change compared to prior"
    };
}
