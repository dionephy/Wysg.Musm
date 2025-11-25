# User Guide: SNOMED-CT Word Count Importer

## Purpose
The Word Count Importer helps you systematically import SNOMED-CT concepts into your global phrase library, filtered by the number of words in their synonyms. This tool searches **ALL SNOMED domains** to find matching terms.

**NEW: Session Persistence** - Your progress is automatically saved! You can close the window anytime and resume exactly where you left off when you reopen it.

## Accessing the Tool
1. Open the Radium application
2. Navigate to **Settings** (gear icon in title bar)
3. Select **Global Phrases** tab
4. Click the **"?? Import by Word Count"** button in the header

## How to Use

### Step 1: Configure Your Search
**Target Word Count:**
- Enter a number between 1 and 10
- Default is 1 (single-word terms)
- Example: "1" finds terms like "heart", "liver", "femur"
- Example: "2" finds terms like "left ventricle", "anterior wall"

**Resuming Previous Session:**
- If you previously started an import, the tool will automatically:
  - Restore your word count target
  - Resume from where you left off
  - Restore statistics (Added, Ignored, Total)
  - Show remaining candidates in queue
- Status message will say "Resumed previous session: X candidates ready, Y already processed"

**Note:** The tool searches across **ALL SNOMED domains** including:
- Body structure (anatomical terms)
- Finding (clinical findings)
- Disorder (diseases and disorders)
- Procedure (medical procedures)
- Observable entity (measurable observations)
- Substance (chemical substances)
- And all other SNOMED concept types

### Step 2: Start Importing
1. Click **"Start Import"** button (or resume from saved session)
2. The tool will:
   - Search ALL SNOMED-CT concepts across all domains
   - Filter for synonyms with exactly your target word count
 - Skip any terms that already exist as global phrases
   - Display the first candidate

### Step 3: Review Each Candidate
For each term, you'll see:
- **Term**: The actual text (e.g., "heart")
- **Concept Info**: SNOMED concept ID and full name
  - Example: `[80891009] | Heart structure (body structure)`

The semantic tag in parentheses (e.g., "body structure", "finding") shows which domain the concept belongs to.

### Step 4: Make Your Choice
**? Add (Active) Button:**
- Adds the term as an **active** global phrase
- Term will appear in completions and syntax highlighting
- Creates SNOMED mapping for clinical context
- Counter updates in Statistics panel
- **Progress automatically saved**

**? Ignore (Inactive) Button:**
- Adds the term as an **inactive** global phrase
- Term is marked as "seen and rejected"
- Won't appear in completions
- Prevents the tool from showing it again
- Useful for non-relevant or low-quality terms
- **Progress automatically saved**

### Step 5: Monitor Progress
The **Statistics Panel** shows:
- **Added**: How many terms you've added (green)
- **Ignored**: How many terms you've ignored (orange)
- **Total Processed**: Combined count (purple)

The **Status Bar** shows:
- Current operation ("Loading more concepts...")
- Progress messages
- Completion message when done
- Session restoration confirmation

### Step 6: Finish or Pause
**To Pause:**
- Simply click **"Close"** button anytime
- Your progress is automatically saved
- Resume later by reopening the window

**To Complete:**
- Continue processing until "Import complete!" message appears
- Session is automatically cleared on completion
- Click **"Close"** when finished
- Global phrases list will automatically refresh

## Session Persistence Features

### Automatic Saving
Your import session is automatically saved:
- After adding a phrase (active)
- After ignoring a phrase (inactive)
- After loading each new page of concepts
- When closing the window

### What Gets Saved
- Target word count setting
- Current pagination token (for efficient resume)
- Queued candidates waiting for review
- Statistics (Added, Ignored, Total counts)
- Current page number and fetching state

### Saved Location
Session file is stored at:
- `%LocalAppData%\Wysg.Musm.Radium\snomed_wordcount_session.json`
- This file is automatically managed - no user action needed

### Clearing Session
Session is automatically cleared when:
- Import completes successfully
- You can manually start fresh by:
  1. Closing the window
  2. Deleting the session file (optional)
  3. Clicking "Start Import" to begin new search

## Tips and Best Practices

### Starting Point
- **Beginners**: Start with single-word (1) terms
- **Advanced**: Progress to 2-3 word terms

### Taking Breaks
- **NEW**: You can now safely close the window and resume later!
- No need to finish in one sitting
- Perfect for processing large sets over multiple sessions
- Your progress and position in the queue are preserved

### Quality Control
- Only **Add** terms you recognize and will use
- **Ignore** terms that are:
  - Too technical or obscure
  - Redundant with existing phrases
  - Not relevant to your specialty

### Understanding Semantic Tags
As you review candidates, you'll see different semantic tags:
- **(body structure)**: Anatomical terms (heart, lung, femur)
- **(finding)**: Clinical observations (fever, pain, swelling)
- **(disorder)**: Diseases (diabetes, hypertension, pneumonia)
- **(procedure)**: Medical procedures (biopsy, surgery, imaging)
- **(observable entity)**: Measurements (blood pressure, temperature)
- **(substance)**: Chemicals and medications

### Efficient Workflow
1. Process all 1-word terms first (fastest, highest value)
2. **Take breaks** - close and resume as needed
3. Review diverse semantic tags as they appear
4. Continue over multiple sessions until complete

## Troubleshooting

### No Candidates Found
- Try a different word count
- Check that Snowstorm service is running
- Most SNOMED concepts are multi-word; single-word terms are less common

### Import Seems Slow
- This is normal - the tool fetches concepts in batches
- Each batch contains up to 50 concepts
- Wait for "Loading more concepts..." to complete
- **You can close the window and resume later**

### Accidentally Added Wrong Term
- Close the importer
- Go to Global Phrases list (main tab area)
- Find the phrase and click "Deactivate" or "Delete"

### Session Not Restoring
- Check Debug output for error messages
- Session file may be corrupted - delete and start fresh
- Ensure `%LocalAppData%\Wysg.Musm.Radium` folder is writable

### Want to Start Fresh
- Close the window
- Delete `%LocalAppData%\Wysg.Musm.Radium\snomed_wordcount_session.json`
- Reopen and click "Start Import"
- OR wait until import completes (session auto-clears)

## Integration with Radium

### Where Added Phrases Appear
- **Editor Completions**: Type to see suggestions
- **Syntax Highlighting**: Terms are colored in editor
- **Global Phrases List**: Manage in Settings �� Global Phrases
- **SNOMED Mappings**: View concept details via "Link SNOMED"

### Phrase Management
After importing, you can:
- **Edit** phrase text
- **Toggle** active/inactive status
- **Delete** unwanted phrases
- **Link** to different SNOMED concepts
- **Convert** account phrases to global

## Advanced Usage

### Multi-Session Import Strategy
Example workflow spanning several days:
1. **Monday AM**: Start 1-word import, process 50 terms, close
2. **Monday PM**: Resume, process another 100 terms, close
3. **Tuesday**: Resume and complete 1-word terms
4. **Wednesday**: Start fresh 2-word import
5. Review and clean up afterward

### Building a Comprehensive Library
Example workflow:
1. Import 1-word terms across all domains (quick wins)
2. Take breaks between sessions as needed
3. Import 2-word terms across all domains
4. Focus on your specialty by accepting relevant semantic tags
5. Import 3-word terms for specific compound terms

### Expected Distribution
Typical SNOMED content distribution by word count:
- **1-word**: ~5-10% of synonyms (basic anatomical/clinical terms)
- **2-word**: ~30-40% of synonyms (common compound terms)
- **3-word**: ~25-30% of synonyms (detailed descriptors)
- **4+ words**: ~25-35% of synonyms (complex clinical descriptions)

## Questions?
Refer to the main Radium documentation or contact support for additional help.

---
**Last Updated**: 2025-11-25  
**Feature Version**: 1.2 (Session Persistence)  
**Compatible with**: Radium production version using Azure SQL
