import { Config, DwFedPermissionList, DwLoginResponse } from '../types';

const API_URL = 'https://dev-api-dw.datachain.cloud';

export const DWLogin = async (publicKey: string, privateKey: string): Promise<DwLoginResponse> => {
    console.log(publicKey, privateKey);
    const body = {
        userPublicKey: publicKey,
        userPrivateKey: privateKey,
    };
    const res = await fetch(API_URL + '/auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(body),
    });
    const json = (await res.json()) as DwLoginResponse;
    if (json.success !== true) throw new Error(JSON.stringify(json));
    return json;
};

export const DWGetPerms = async (config: Config): Promise<DwFedPermissionList> => {
    const { payload } = await DWLogin(config.operatorDwPublicKey, config.operatorDwPrivateKey);
    const res = await fetch(API_URL + '/permission/list/granted', {
        headers: {
            'Content-Type': 'application/json',
            Authorization: 'Bearer ' + payload.token,
        },
    });
    const json = (await res.json()) as DwFedPermissionList;
    if (json.success !== true) throw new Error(JSON.stringify(json));
    return json;
};

export const DWCheckPerms = async (config: Config, publicKey: string): Promise<boolean> => {
    const permsWithFed = await DWGetPerms(config);
    const permArray = permsWithFed.payload.list;
    const perm = permArray.find(perm => perm.receiver_username === publicKey && perm.folder_id === 66);
    if (perm) return true;
    else return false;
};

export const DWGetFile = async (publicKey?: string, privateKey?: string, fileName?: string): Promise<string> => {
    const { payload } = await DWLogin(publicKey, privateKey);
    const path = fileName ? fileName : 'Apps/JSON Logger/test.json';
    const res = await fetch(API_URL + '/file/download?path=' + path, {
        headers: {
            Authorization: 'Bearer ' + payload.token,
        },
    });
    const text = (await res.text()) as string;
    return text;
};
