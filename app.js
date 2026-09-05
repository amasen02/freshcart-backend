'use strict';
document.querySelectorAll('[data-copy-target]').forEach(button => button.addEventListener('click', async () => {
  const target=document.getElementById(button.dataset.copyTarget);
  let copied=false;
  try { await navigator.clipboard.writeText(target.textContent); copied=true; }
  catch { const range=document.createRange(); range.selectNodeContents(target); const selection=window.getSelection(); selection.removeAllRanges(); selection.addRange(range); if (document.execCommand) copied=document.execCommand('copy'); }
  const status=document.getElementById(button.dataset.statusTarget);
  status.querySelectorAll('[data-copy-result]').forEach(el=>{el.hidden=el.dataset.copyResult!==(copied?'success':'manual');});
}));
const reduce=matchMedia('(prefers-reduced-motion: reduce)');
const load=image=>{const src=image.dataset.motionSrc||image.dataset.lazySrc;if(src&&!image.getAttribute('src'))image.src=src;};
const observer='IntersectionObserver' in window?new IntersectionObserver(entries=>entries.forEach(entry=>{if(entry.isIntersecting&&(!entry.target.dataset.motionSrc||!reduce.matches))load(entry.target);}),{rootMargin:'300px'}):null;
document.querySelectorAll('img[data-lazy-src],img[data-motion-src]').forEach(image=>{
  if(observer)observer.observe(image);else if(!image.dataset.motionSrc||!reduce.matches)load(image);
});
document.querySelectorAll('[data-play-target]').forEach(button=>button.addEventListener('click',()=>load(document.getElementById(button.dataset.playTarget))));
reduce.addEventListener('change',event=>{if(event.matches)document.querySelectorAll('img[data-motion-src]').forEach(image=>image.removeAttribute('src'));});
