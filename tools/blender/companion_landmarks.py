"""Landmarks from the four gallery sculpts, in their unscaled Blender coordinates."""

from companion_shoes import SHOES

# Each leg has hip, knee, ankle and toe coordinates. Each arm has shoulder, elbow,
# wrist and fingertip coordinates. Left and right are the character's own sides.
PROFILES = {
    'summer_huajian': {
        'sole': .091, 'head_height': .717,
        'torso': ((.025,.015,.427),(.028,-.010,.569),(.025,-.033,.719)),
        'L': {
            'leg': ((.061,.016,.429),(.059,-.045,.286),(.066,.045,.139),(.056,-.068,.100)),
            'arm': ((.078,-.025,.680),(.142,-.033,.591),(.209,-.065,.535),(.248,-.075,.525)),
            'curl': ((.151,.088,.803),(.175,.108,.712)),
            'hem': ((.072,.072,.507),(.172,.207,.348)),
        },
        'R': {
            'leg': ((-.025,.017,.424),(-.039,-.057,.294),(-.044,.028,.145),(-.070,-.083,.100)),
            'arm': ((-.041,-.036,.678),(-.084,-.057,.594),(-.174,-.158,.631),(-.196,-.169,.637)),
            'curl': ((-.143,.072,.799),(-.164,.112,.712)),
            'hem': ((-.046,.057,.510),(-.143,.177,.329)),
        },
    },
    'winter_xueling': {
        'sole': .056, 'head_height': .729,
        'torso': ((.015,-.007,.442),(.018,-.018,.567),(.014,-.034,.734)),
        'L': {
            'leg': ((.055,-.011,.450),(.054,-.047,.287),(.057,.005,.131),(.060,-.067,.074)),
            'arm': ((.066,-.033,.679),(.123,-.049,.568),(.201,-.059,.495),(.231,-.073,.479)),
            'curl': ((.163,.070,.829),(.132,.088,.739)),
            'hem': ((.063,.017,.528),(.163,.049,.402)),
        },
        'R': {
            'leg': ((-.032,-.009,.444),(-.036,-.045,.289),(-.034,.009,.134),(-.042,-.069,.074)),
            'arm': ((-.054,-.025,.684),(-.100,-.052,.566),(-.041,-.125,.665),(-.026,-.130,.689)),
            'curl': ((-.161,.067,.827),(-.155,.083,.750)),
            'hem': ((-.058,.021,.525),(-.155,.052,.401)),
        },
    },
    'hidden_shijian': {
        'sole': .059, 'head_height': .728,
        'torso': ((-.005,-.006,.429),(-.013,-.029,.574),(-.021,-.058,.733)),
        'L': {
            'leg': ((.042,-.010,.426),(.037,-.054,.265),(.037,-.011,.106),(.042,-.076,.075)),
            'arm': ((.072,-.040,.673),(.099,-.055,.573),(.043,-.151,.597),(.011,-.168,.622)),
            'curl': ((.142,.043,.763),(.190,.174,.344)),
            'hem': ((.066,-.026,.425),(.161,.027,.177)),
        },
        'R': {
            'leg': ((-.043,-.006,.427),(-.045,-.056,.266),(-.039,-.013,.107),(-.045,-.075,.075)),
            'arm': ((-.098,-.055,.677),(-.107,-.075,.568),(-.048,-.162,.594),(-.022,-.178,.626)),
            'curl': ((-.151,.045,.766),(-.192,.197,.352)),
            'hem': ((-.082,-.025,.426),(-.185,.024,.174)),
        },
    },
    'hidden_xingmian': {
        'sole': .059, 'head_height': .720,
        'torso': ((-.019,-.020,.424),(-.025,-.041,.568),(-.039,-.074,.726)),
        'L': {
            'leg': ((.027,-.031,.428),(.037,-.069,.258),(.037,-.023,.107),(.043,-.084,.061)),
            'arm': ((.050,-.063,.670),(.085,-.096,.557),(.022,-.193,.600),(-.014,-.205,.632)),
            'curl': ((.148,.052,.753),(.214,.176,.360)),
            'hem': ((.054,-.058,.437),(.160,.047,.178)),
        },
        'R': {
            'leg': ((-.057,-.030,.425),(-.057,-.070,.265),(-.048,-.046,.111),(-.053,-.117,.073)),
            'arm': ((-.113,-.069,.672),(-.122,-.113,.556),(-.066,-.193,.598),(-.039,-.206,.623)),
            'curl': ((-.163,.043,.756),(-.213,.174,.360)),
            'hem': ((-.085,-.056,.441),(-.182,.042,.169)),
        },
    },
}

def distance_to_bone(point, bone):
    axis = bone.tail_local-bone.head_local
    t = max(0, min(1, (point-bone.head_local).dot(axis)/axis.length_squared))
    return (point-bone.head_local-t*axis).length

def candidates_for(role, profile, point, arm):
    x,y,z = point
    center=profile['torso'][0][0]
    side='L' if x>center else 'R'
    # The arms never own the face. Long hair flanks are far behind the cheeks.
    if z >= profile['head_height']:
        return [('Head',1)]
    loose_hair = role.startswith('hidden_') and abs(x-center)>.118 and y>.012
    if loose_hair and z>.205:
        return [('Head',.22),('Curl'+side,2),('Hem'+side,.40)]
    if role=='summer_huajian' and x<-.126 and y<-.083 and .234<z<.65:
        if z<.60 or distance_to_bone(point,arm.bones['PalmR']) < .055:
            return [('PalmR',1)]
    if z < .435:
        # Unselected lower islands are skirt, ribbons, or decoration. Only the
        # labelled shoe and leg islands get lower-leg weights.
        top,_,_=SHOES[role]
        if not role.startswith('hidden_') and z>=top:
            thigh='Thigh'+side
            distance=distance_to_bone(point,arm.bones[thigh])
            if distance<.043:
                return [(thigh,1),('Hips',.45),('Hem'+side,.20)]
        return [('Hem'+side,1),('Hips',.22)]
    names=[('Chest',1),('Hips',1),('Head',.6),('Hem'+side,.7)]
    if z>=profile['head_height']-.026:
        return [('Head',3),('Chest',.22)]
    for name in ('UpperArm','Forearm','Palm'):
        bone=name+side
        distance=distance_to_bone(point,arm.bones[bone])
        influence=max(0,min(1,(.087-distance)/.048))
        if influence>0:
            names.append((bone,influence))
    return names
