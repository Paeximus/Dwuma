// =============================================
// onboarding.js  —  AI Career Agent
// Multi-step onboarding form logic
// =============================================

const API_BASE = 'http://localhost:5000/api';
const TOTAL_STEPS = 6;
let currentStep = 1;

// ─── TAG INPUT INSTANCES ─────────────────────────────────────────────────────

const tagStores = {
  certs:      { tags: [], wrapperId: 'certsWrapper',     inputId: 'certInput' },
  techSkills: { tags: [], wrapperId: 'techSkillsWrapper', inputId: 'techSkillInput' },
  softSkills: { tags: [], wrapperId: 'softSkillsWrapper', inputId: 'softSkillInput' },
  languages:  { tags: [], wrapperId: 'languagesWrapper',  inputId: 'langInput' },
};

Object.entries(tagStores).forEach(([key, store]) => {
  initTagInput(store);
});

function initTagInput(store) {
  const input   = document.getElementById(store.inputId);
  const wrapper = document.getElementById(store.wrapperId);

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      addTag(store, input.value.trim().replace(/,$/, ''));
    }
    if (e.key === 'Backspace' && input.value === '' && store.tags.length > 0) {
      store.tags.pop();
      renderTags(store);
    }
  });

  wrapper.addEventListener('click', () => input.focus());
}

function addTag(store, value) {
  if (!value || store.tags.map(t => t.toLowerCase()).includes(value.toLowerCase())) return;
  store.tags.push(value);
  renderTags(store);
  document.getElementById(store.inputId).value = '';
}

function removeTag(storeKey, index) {
  tagStores[storeKey].tags.splice(index, 1);
  renderTags(tagStores[storeKey]);
}

function renderTags(store) {
  const wrapper = document.getElementById(store.wrapperId);
  wrapper.querySelectorAll('.tag').forEach(t => t.remove());
  const input = document.getElementById(store.inputId);

  store.tags.forEach((tag, i) => {
    const storeKey = Object.keys(tagStores).find(k => tagStores[k] === store);
    const el = document.createElement('div');
    el.className = 'tag';
    el.innerHTML = `${escHtml(tag)} <button onclick="removeTag('${storeKey}', ${i})" title="Remove">×</button>`;
    wrapper.insertBefore(el, input);
  });
}

// ─── PREFERENCE CHIPS ────────────────────────────────────────────────────────

const selectedPrefs = { industries: [], jobTypes: [] };

document.querySelectorAll('.pref-chip').forEach(chip => {
  chip.addEventListener('click', () => {
    const group = chip.dataset.group;
    const value = chip.textContent.trim();
    chip.classList.toggle('chosen');

    if (chip.classList.contains('chosen')) {
      if (!selectedPrefs[group].includes(value)) selectedPrefs[group].push(value);
    } else {
      selectedPrefs[group] = selectedPrefs[group].filter(v => v !== value);
    }
  });
});

// ─── FILE UPLOAD ─────────────────────────────────────────────────────────────

let uploadedFile = null;

const uploadZone = document.getElementById('uploadZone');
const fileInput  = document.getElementById('cvFile');

uploadZone.addEventListener('click', () => fileInput.click());

uploadZone.addEventListener('dragover', (e) => {
  e.preventDefault();
  uploadZone.classList.add('dragover');
});

uploadZone.addEventListener('dragleave', () => uploadZone.classList.remove('dragover'));

uploadZone.addEventListener('drop', (e) => {
  e.preventDefault();
  uploadZone.classList.remove('dragover');
  const file = e.dataTransfer.files[0];
  if (file) handleFile(file);
});

fileInput.addEventListener('change', () => {
  if (fileInput.files[0]) handleFile(fileInput.files[0]);
});

function handleFile(file) {
  const allowed = ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
  if (!allowed.includes(file.type)) {
    alert('Please upload a PDF or DOCX file.');
    return;
  }
  if (file.size > 5 * 1024 * 1024) {
    alert('File must be smaller than 5MB.');
    return;
  }
  uploadedFile = file;
  document.getElementById('uploadedFileName').textContent = file.name;
  document.getElementById('uploadedFileSize').textContent = formatBytes(file.size);
  document.getElementById('uploadSuccess').style.display = 'flex';
  uploadZone.style.display = 'none';
}

function clearUpload() {
  uploadedFile = null;
  fileInput.value = '';
  document.getElementById('uploadSuccess').style.display = 'none';
  uploadZone.style.display = 'block';
}

// ─── STEP NAVIGATION ─────────────────────────────────────────────────────────

function nextStep() {
  const error = validateStep(currentStep);
  if (error) { showStepError(error); return; }

  if (currentStep === 5) buildReviewCard();

  if (currentStep === TOTAL_STEPS) {
    submitProfile();
    return;
  }

  goToStep(currentStep + 1);
}

function prevStep() {
  if (currentStep > 1) goToStep(currentStep - 1);
}

function goToStep(n) {
  // Mark previous steps as done
  for (let i = 1; i <= TOTAL_STEPS; i++) {
    const nav = document.getElementById(`nav-${i}`);
    const dot = document.getElementById(`dot-${i}`);
    nav.classList.remove('active', 'done');
    if (i < n) { nav.classList.add('done'); dot.textContent = '✓'; }
  }

  // Activate panel
  document.querySelectorAll('.step-panel').forEach(p => p.classList.remove('active'));
  document.getElementById(`step-${n}`).classList.add('active');
  document.getElementById(`nav-${n}`).classList.add('active');

  currentStep = n;

  // Update buttons
  document.getElementById('prevBtn').style.display = n > 1 ? 'inline-flex' : 'none';
  document.getElementById('nextBtn').textContent =
    n === TOTAL_STEPS ? '🚀 Save Profile' : 'Continue →';
  document.getElementById('stepCounter').textContent = `Step ${n} of ${TOTAL_STEPS}`;
}

// ─── VALIDATION ──────────────────────────────────────────────────────────────

function validateStep(step) {
  const req = (id) => document.getElementById(id)?.value?.trim();
  switch (step) {
    case 1:
      if (!req('firstName'))  return 'Please enter your first name.';
      if (!req('lastName'))   return 'Please enter your last name.';
      if (!req('email') || !req('email').includes('@')) return 'Please enter a valid email address.';
      break;
    case 2:
      if (!req('institution'))  return 'Please enter your institution.';
      if (!req('degreeLevel'))  return 'Please select your degree level.';
      if (!req('fieldOfStudy')) return 'Please enter your field of study.';
      break;
    case 3:
      if (tagStores.techSkills.tags.length === 0) return 'Please add at least one technical skill.';
      break;
    case 4:
      if (selectedPrefs.industries.length === 0) return 'Please select at least one industry of interest.';
      break;
    default:
      break;
  }
  return null;
}

function showStepError(message) {
  // Remove existing error
  const existing = document.querySelector('.step-inline-error');
  if (existing) existing.remove();

  const el = document.createElement('div');
  el.className = 'alert alert-error step-inline-error';
  el.textContent = message;
  el.style.marginTop = '12px';
  document.getElementById(`step-${currentStep}`).appendChild(el);

  setTimeout(() => el.remove(), 4000);
}

// ─── REVIEW CARD ─────────────────────────────────────────────────────────────

function buildReviewCard() {
  const p = collectProfile();
  const rows = [
    ['Name',        `${p.firstName} ${p.lastName}`],
    ['Email',       p.email],
    ['Location',    p.location || '—'],
    ['Education',   `${p.degreeLevel} in ${p.fieldOfStudy}, ${p.institution}`],
    ['Graduation',  p.graduationYear || '—'],
    ['Experience',  expLabel(p.yearsExp)],
    ['Industries',  p.industries.join(', ') || '—'],
    ['Job Types',   p.jobTypes.join(', ') || '—'],
    ['Tech Skills', p.techSkills.join(', ') || '—'],
    ['Languages',   p.languages.join(', ') || '—'],
    ['CV Uploaded', uploadedFile ? uploadedFile.name : 'No — skip for now'],
  ];

  document.getElementById('reviewCard').innerHTML = rows.map(([k, v]) => `
    <div class="summary-row">
      <div class="summary-key">${k}</div>
      <div class="summary-val">${escHtml(v)}</div>
    </div>`).join('');
}

// ─── COLLECT & SUBMIT ─────────────────────────────────────────────────────────

function collectProfile() {
  return {
    firstName:      val('firstName'),
    lastName:       val('lastName'),
    email:          val('email'),
    phone:          val('phone'),
    location:       val('location'),
    linkedin:       val('linkedin'),
    institution:    val('institution'),
    degreeLevel:    val('degreeLevel'),
    fieldOfStudy:   val('fieldOfStudy'),
    graduationYear: val('graduationYear'),
    classification: val('classification'),
    certifications: tagStores.certs.tags,
    techSkills:     tagStores.techSkills.tags,
    softSkills:     tagStores.softSkills.tags,
    languages:      tagStores.languages.tags,
    yearsExp:       val('yearsExp'),
    industries:     selectedPrefs.industries,
    jobTypes:       selectedPrefs.jobTypes,
    workLocation:   val('workLocation'),
    salaryRange:    val('salaryRange'),
    careerGoal:     val('careerGoal'),
  };
}

async function submitProfile() {
  const btn = document.getElementById('nextBtn');
  btn.disabled = true;
  btn.textContent = 'Saving...';

  const profile = collectProfile();

  try {
    // 1. Save profile
    const res = await fetch(`${API_BASE}/profile`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(profile),
    });

    if (!res.ok) throw new Error((await res.json()).message || 'Save failed.');
    const { userId } = await res.json();

    // 2. Upload CV if provided
    if (uploadedFile && userId) {
      const fd = new FormData();
      fd.append('file', uploadedFile);
      fd.append('userId', userId);
      await fetch(`${API_BASE}/profile/upload-cv`, { method: 'POST', body: fd });
    }

    // 3. Persist userId locally (replace with JWT in production)
    localStorage.setItem('userId', userId);
    localStorage.setItem('userProfile', JSON.stringify(profile));

    document.getElementById('saveSuccess').style.display = 'block';
    setTimeout(() => { window.location.href = 'dashboard.html'; }, 2000);

  } catch (err) {
    document.getElementById('saveError').textContent = err.message;
    document.getElementById('saveError').style.display = 'block';
    btn.disabled = false;
    btn.textContent = '🚀 Save Profile';
  }
}

// ─── HELPERS ─────────────────────────────────────────────────────────────────

function val(id) { return document.getElementById(id)?.value?.trim() ?? ''; }
function escHtml(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
function formatBytes(b) {
  if (b < 1024) return `${b} B`;
  if (b < 1024 * 1024) return `${(b/1024).toFixed(1)} KB`;
  return `${(b/1024/1024).toFixed(1)} MB`;
}
function expLabel(val) {
  const map = { '0':'Fresh Graduate', '1':'< 1 year', '2':'1–2 years', '3':'3–5 years', '5':'5+ years' };
  return map[val] ?? val;
}
