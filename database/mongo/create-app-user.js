// Usuario de la aplicación en Mongo con mínimo privilegio: readWrite solo sobre ate_nosql_db.
// Uso (la contraseña llega por variable, nunca escrita aquí):
//   mongosh "mongodb://<root>:<pwd>@<host>:27017/?authSource=admin" \
//     --eval "const appPassword='$MONGO_APP_PASSWORD'" database/mongo/create-app-user.js
if (typeof appPassword === 'undefined' || !appPassword) {
  throw new Error('Falta appPassword');
}

const appDb = db.getSiblingDB('ate_nosql_db');
const roles = [{ role: 'readWrite', db: 'ate_nosql_db' }];

if (appDb.getUser('ate_app')) {
  appDb.updateUser('ate_app', { pwd: appPassword, roles });
  print('ate_app actualizado');
} else {
  appDb.createUser({ user: 'ate_app', pwd: appPassword, roles });
  print('ate_app creado');
}
