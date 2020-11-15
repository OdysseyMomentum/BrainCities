import { namespace } from 'store';

export interface Config {
    operatorDwPublicKey?: string;
    operatorDwPrivateKey?: string;
    smartLights?: {
        -readonly [key in keyof typeof SMART_LIGHT_IDS]?: {
            publicKey?: string;
            privateKey?: string;
            xValue?: string;
            yValue?: string;
        };
    };
}

export enum SMART_LIGHT_IDS {
    SL1 = 'Smart-Light-1',
    SL2 = 'Smart-Light-2',
    SL3 = 'Smart-Light-3',
}

export interface DwLoginResponse {
    success?: boolean;
    payload?: {
        token?: string;
        role?: string;
    };
}

export interface DwFedPermissionList {
    success?: boolean;
    payload?: {
        list?: [
            {
                permission_id?: number;
                last_modified?: string;
                type?: string;
                sender_id?: number;
                sender_group_id?: number;
                sender_group_name?: string;
                group_public_key?: string;
                sender_first_name?: string;
                sender_last_name?: string;
                sender_username?: string;
                folder_id?: number;
                folder_name?: string;
                allowed?: number;
                receiver_user_id?: number;
                receiver_username?: string;
                receiver_first_name?: string;
                receiver_last_name?: string;
                receiver_group_id?: number;
                receiver_group_name?: string;
                receiver_group_public_key?: string;
            },
        ];
    };
}
