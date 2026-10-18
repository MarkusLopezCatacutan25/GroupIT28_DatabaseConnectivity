const fs = require('fs');
const path = require('path');
const {initializeTestEnvironment, assertSucceeds, assertFails} = require('@firebase/rules-unit-testing');
const {ref, set, get, query, orderByChild, limitToLast, remove} = require('firebase/database');
(async () => {
  const env = await initializeTestEnvironment({projectId:'demo-savorpinoy-activity5', database:{host:'127.0.0.1',port:9000,rules:fs.readFileSync('database.rules.json','utf8')}});
  const results=[];
  async function check(name,promise) {await promise;results.push(name);console.log('PASS '+results.length+': '+name);}
  const owner=env.authenticatedContext('chef-a').database(), other=env.authenticatedContext('chef-b').database(), guest=env.unauthenticatedContext().database();
  let serial=1;const key=()=> 'slice_'+(serial++).toString(16).padStart(32,'0');
  const valid={player_name:'Activity5Chef',score:9375,dish_name:'Adobo sa Dilaw',level:'Batangas',remarks:'batangas_slice_v1 | 3 stars',created_at:'2026-10-18T12:00:00.0000000Z',owner_uid:'chef-a'};
  const id=key(),path='player_scores/'+id;
  await check('owner creates valid record',assertSucceeds(set(ref(owner,path),valid)));
  await check('unauthenticated creation denied',assertFails(set(ref(guest,'player_scores/'+key()),valid)));
  await check('forged owner denied',assertFails(set(ref(other,'player_scores/'+key()),valid)));
  await check('overwrite denied',assertFails(set(ref(owner,path),{...valid,score:1})));
  await check('deletion denied',assertFails(remove(ref(owner,path))));
  for(const [label,patch] of [['negative score',{score:-1}],['score over 9375',{score:9376}],['fractional score',{score:1.5}],['string score',{score:'100'}],['empty nickname',{player_name:''}],['long nickname',{player_name:'x'.repeat(25)}],['control characters',{player_name:'a\nb'}],['wrong dish',{dish_name:'Other'}],['wrong stage',{level:'Other'}],['wrong timestamp type',{created_at:123}],['wrong timestamp format',{created_at:'2026-10-18'}],['invalid remarks',{remarks:'Master chef'}],['extra field',{extra:'no'}]]) {
    await check(label+' denied',assertFails(set(ref(owner,'player_scores/'+key()),{...valid,...patch})));
  }
  const missing={...valid};delete missing.owner_uid;
  await check('missing ownership denied',assertFails(set(ref(owner,'player_scores/'+key()),missing)));
  await check('invalid key denied',assertFails(set(ref(owner,'player_scores/not-a-run-id'),valid)));
  await check('zero score allowed',assertSucceeds(set(ref(owner,'player_scores/'+key()),{...valid,score:0})));
  await check('unbounded leaderboard denied',assertFails(get(ref(owner,'player_scores'))));
  await check('unauthenticated leaderboard denied',assertFails(get(query(ref(guest,'player_scores'),orderByChild('score'),limitToLast(10)))));
  await check('oversized leaderboard denied',assertFails(get(query(ref(owner,'player_scores'),orderByChild('score'),limitToLast(11)))));
  const board=await get(query(ref(owner,'player_scores'),orderByChild('score'),limitToLast(10)));
  await check('top 10 contains same record',Promise.resolve().then(()=>{if(board.child(id).val().score!==valid.score)throw Error('Mismatch');}));
  const reopened=env.authenticatedContext('chef-a').database(), again=await get(ref(reopened,path));
  await check('new client retrieves same record',Promise.resolve().then(()=>{if(JSON.stringify(again.val())!==JSON.stringify(board.child(id).val()))throw Error('Mismatch');}));
  const output=process.env.ACTIVITY5_TEST_OUTPUT || 'Docs/Activity5/firebase-rules-results.json';
  fs.mkdirSync(path.dirname(output),{recursive:true});
  fs.writeFileSync(output,JSON.stringify({scope:'LOCAL EMULATOR ONLY; not live Firebase or Unity cloud evidence',passed:results.length,results,matchingRecord:{score_id:id,...again.val()}},null,2));
  console.log('COMPLETE '+results.length+' rules checks passed.');await env.cleanup();process.exit(0);
})().catch(e=>{console.error(e);process.exit(1);});
