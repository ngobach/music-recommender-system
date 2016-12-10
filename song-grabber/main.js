const
    co = require('co'),
    colors = require('colors'),
    del = require('del')
    path = require('path'),
    promisify = require('promisify-node'),
    fs = promisify('fs'),
    rp = require('request-promise'),
    _ = require('lodash');

const
    USER_COUNT = 40,
    SONGS_PER_USER = 10,
    TEST_USER_COUNT = 10,
    TEST_SONGS_PER_USER = 5;

/**
 * Entry point
 */
co(function*() {
    yield del('./data/**');
    yield fs.mkdir('data');
    const songs = JSON.parse(yield rp('http://mp3.zing.vn/json/song/get-top-100?start=0&length=100&id=IWZ9Z088')).data.songs.map((song,idx) => ({id: idx + 1, name: `${song.name} - ${song.artist}`}));
    yield fs.writeFile(path.join('data', 'songs.json'), JSON.stringify(songs, null, 4));
    const ids = songs.map(s => s.id);
    const rela = [];
    const uids = [];
    for (let i = 1;i <= USER_COUNT; i++) {
        uids.push(i);
        _(ids).shuffle().take(SONGS_PER_USER).forEach(id => {
            rela.push({
                user_id: i,
                song_id: id
            });
        });
    }
    yield fs.writeFile(path.join('data', 'relations.json'), JSON.stringify(rela, null, 4));
    const tests = [];
    _(uids).shuffle().take(TEST_USER_COUNT).each(uid => {
        _(ids).shuffle().take(TEST_SONGS_PER_USER).forEach(id => {
            tests.push({
                user_id: uid,
                song_id: id
            });
        });
    });
    yield fs.writeFile(path.join('data', 'tests.json'), JSON.stringify(tests, null, 4));
    console.log('Done~!'.red);
}).catch(e => console.error);
