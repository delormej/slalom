const express = require('express');
const app = express();
const http = require('http');
const path = require('path');
const request = require('request');

app.get('/', function(req, res) {
    //res.send('hello, world.');
    res.sendFile('index.html', { root: __dirname + '/views/', });
});

app.get('/videos', function(req, res) {
    return request.get('http://ski-app.azurewebsites.net/api/list');
});

app.set('port', 3000);

http.createServer(app).listen(app.get('port'), function() {
    console.log('Express server listening on port ' + app.get('port'));
});