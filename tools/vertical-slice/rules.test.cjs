// LOCAL EMULATOR ONLY. These rule tests do not constitute deployed Firebase evidence.
const fs=require('fs');
const {initializeTestEnvironment,assertSucceeds,assertFails}=require('@firebase/rules-unit-testing');
const {ref,update,get,query,orderByChild,limitToLast,set,remove}=require('firebase/database');
(async()=>{
 const env=await initializeTestEnvironment({projectId:'demo-savorpinoy-vertical-slice',database:{host:'127.0.0.1',port:9000,rules:fs.readFileSync('database.rules.json','utf8')}});
 const results=[];let serial=0;
 const id=()=> 'attempt_'+(++serial).toString(16).padStart(32,'0');
 const a=env.authenticatedContext('chef-a').database(),b=env.authenticatedContext('chef-b').database(),guest=env.unauthenticatedContext().database();
 const valid={dish_id:'Batangas_AdoboSaDilaw',score:950,is_correct:true,remarks:'Master Searing',created_at:'2026-10-27T10:05:00Z'};
 const stamp='2026-10-27T10:05:00Z';
 const payload=(key,attempt=valid,uid='chef-a',board=true)=>{
  const p={};p[`users/${uid}/display_name`]='ChefMaria';p[`users/${uid}/updated_at`]=stamp;p[`users/${uid}/attempts/${key}`]=attempt;
  if(board)p[`leaderboard/${key}`]={uid,player_name:'ChefMaria',dish_id:attempt.dish_id,score:attempt.score,created_at:attempt.created_at};return p;
 };
 async function check(label,task){await task;results.push(label);console.log('PASS '+results.length+' '+label);}
 const quezon=id();await check('Quezon owner atomic write succeeds',assertSucceeds(update(ref(a),payload(quezon,{...valid,dish_id:'Quezon_PancitHabhab'}))));
 await check('Quezon private attempt read succeeds',assertSucceeds(get(ref(a,`users/chef-a/attempts/${quezon}`))));
 await check('dish of an existing attempt cannot change',assertFails(update(ref(a),payload(quezon,valid))));
 const key=id(),atomic=payload(key);
 await check('owner atomic profile + attempt + leaderboard create',assertSucceeds(update(ref(a),atomic)));
 await check('same-ID same-payload retry acknowledged',assertSucceeds(update(ref(a),atomic)));
 await check('guest write rejected',assertFails(update(ref(guest),payload(id()))));
 await check('cross-UID write rejected',assertFails(update(ref(b),payload(id()))));
 await check('owner loads same exact attempt',assertSucceeds(get(ref(a,`users/chef-a/attempts/${key}`))).then(s=>{if(JSON.stringify(s.val())!==JSON.stringify(Object.fromEntries(Object.entries(valid).sort())))throw Error('Read-back mismatch');}));
 await check('another UID cannot read private attempt',assertFails(get(ref(b,`users/chef-a/attempts/${key}`))));
 await check('guest cannot read private attempt',assertFails(get(ref(guest,`users/chef-a/attempts/${key}`))));
 await check('attempt deletion rejected',assertFails(remove(ref(a,`users/chef-a/attempts/${key}`))));
 await check('attempt score overwrite rejected',assertFails(update(ref(a),payload(key,{...valid,score:951}))));
 await check('leaderboard ownership overwrite rejected',assertFails(set(ref(b,`leaderboard/${key}`),{uid:'chef-b',player_name:'Other',dish_id:valid.dish_id,score:950,created_at:stamp})));
 await check('leaderboard deletion rejected',assertFails(remove(ref(a,`leaderboard/${key}`))));
 for(const [label,patch] of [['negative score',{score:-1}],['score over ceiling',{score:9376}],['fractional score',{score:1.1}],['string score',{score:'950'}],['wrong dish',{dish_id:'Other'}],['string correctness',{is_correct:'true'}],['empty remarks',{remarks:''}],['long remarks',{remarks:'x'.repeat(121)}],['bad timestamp',{created_at:'yesterday'}],['numeric timestamp',{created_at:123}],['unknown field',{extra:true}]])
  await check(label+' rejected',assertFails(update(ref(a),payload(id(),{...valid,...patch}))));
 for(const field of Object.keys(valid)){const missing={...valid};delete missing[field];await check('missing '+field+' rejected',assertFails(update(ref(a),payload(id(),missing,'chef-a',false))));}
 await check('unknown profile field rejected',assertFails(set(ref(a,'users/chef-a/admin'),true)));
 await check('whole-user deletion rejected',assertFails(remove(ref(a,'users/chef-a'))));
 await check('profile name cannot be blank',assertFails(set(ref(a,'users/chef-a/display_name'),'')));
 await check('profile name control character rejected',assertFails(set(ref(a,'users/chef-a/display_name'),'Chef\nMaria')));
 await check('invalid attempt key rejected',assertFails(update(ref(a),payload('nickname-as-id'))));
 const wrong=payload(id());Object.values(wrong).find(v=>v&&v.uid).score=951;
 await check('leaderboard cannot disagree with attempt',assertFails(update(ref(a),wrong)));
 const absent=id();await check('orphan leaderboard row rejected',assertFails(set(ref(a,`leaderboard/${absent}`),{uid:'chef-a',player_name:'ChefMaria',dish_id:valid.dish_id,score:950,created_at:stamp})));
 await check('failed attempt cannot enter leaderboard',assertFails(update(ref(a),payload(id(),{...valid,is_correct:false,score:0}))));
 await check('failed attempt without ranking is valid history',assertSucceeds(update(ref(a),payload(id(),{...valid,is_correct:false,score:0},'chef-a',false))));
 await check('maximum score accepted',assertSucceeds(update(ref(a),payload(id(),{...valid,score:9375}))));
 await check('legacy flat list writes denied',assertFails(set(ref(a,'player_scores/legacy'),valid)));
 await check('unbounded leaderboard rejected',assertFails(get(ref(a,'leaderboard'))));
 await check('oversized leaderboard rejected',assertFails(get(query(ref(a,'leaderboard'),orderByChild('score'),limitToLast(11)))));
 await check('unauthenticated leaderboard rejected',assertFails(get(query(ref(guest,'leaderboard'),orderByChild('score'),limitToLast(10)))));
 await check('bounded leaderboard succeeds',assertSucceeds(get(query(ref(a,'leaderboard'),orderByChild('score'),limitToLast(10)))));
 await check('bounded own history succeeds',assertSucceeds(get(query(ref(a,'users/chef-a/attempts'),orderByChild('created_at'),limitToLast(20)))));
 await check('bounded other UID history rejected',assertFails(get(query(ref(b,'users/chef-a/attempts'),orderByChild('created_at'),limitToLast(20)))));
 await check('unbounded history rejected',assertFails(get(ref(a,'users/chef-a/attempts'))));
 await check('empty own history is valid data',assertSucceeds(get(query(ref(b,'users/chef-b/attempts'),orderByChild('created_at'),limitToLast(20)))).then(s=>{if(s.exists())throw Error('Expected empty');}));
 fs.writeFileSync(process.env.VERTICAL_SLICE_REPORT||'rules-results.json',JSON.stringify({scope:'LOCAL EMULATOR ONLY',passed:results.length,results},null,2));
 await env.cleanup();console.log('COMPLETE '+results.length);process.exit(0);
})().catch(e=>{console.error(e);process.exit(1);});
